using WorkflowEngine.DomainModels;
using WorkflowEngine.DataAccess;

namespace WorkflowEngine.BusinessLogic
{
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public interface IWorkflowValidator
    {
        Task ValidateDefinitionAsync(WorkflowDefinition definition);
        Task ValidateActionExecutionAsync(WorkflowInstance instance, WorkflowAction action, WorkflowDefinition definition);
    }

    public class WorkflowValidator : IWorkflowValidator
    {
        public Task ValidateDefinitionAsync(WorkflowDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new ValidationException("Definition name is required");

            if (!definition.States.Any())
                throw new ValidationException("Definition must have at least one state");

            // Check for duplicate state IDs
            var stateIds = definition.States.Select(s => s.Id).ToList();
            if (stateIds.Count != stateIds.Distinct().Count())
                throw new ValidationException("Duplicate state IDs found");

            // Check for duplicate action IDs
            var actionIds = definition.Actions.Select(a => a.Id).ToList();
            if (actionIds.Count != actionIds.Distinct().Count())
                throw new ValidationException("Duplicate action IDs found");

            // Exactly one initial state
            var initialStates = definition.States.Where(s => s.IsInitial).ToList();
            if (initialStates.Count != 1)
                throw new ValidationException("Definition must have exactly one initial state");

            // Validate actions reference valid states
            foreach (var action in definition.Actions)
            {
                if (!stateIds.Contains(action.ToState))
                    throw new ValidationException($"Action '{action.Id}' references unknown target state '{action.ToState}'");

                foreach (var fromState in action.FromStates)
                {
                    if (!stateIds.Contains(fromState))
                        throw new ValidationException($"Action '{action.Id}' references unknown source state '{fromState}'");
                }
            }

            return Task.CompletedTask;
        }

        public Task ValidateActionExecutionAsync(WorkflowInstance instance, WorkflowAction action, WorkflowDefinition definition)
        {
            if (!action.Enabled)
                throw new ValidationException($"Action '{action.Id}' is disabled");

            if (!action.FromStates.Contains(instance.CurrentStateId))
                throw new ValidationException($"Action '{action.Id}' cannot be executed from current state '{instance.CurrentStateId}'");

            var currentState = definition.States.First(s => s.Id == instance.CurrentStateId);
            if (currentState.IsFinal)
                throw new ValidationException($"Cannot execute actions on final state '{currentState.Id}'");

            return Task.CompletedTask;
        }
    }

    public interface IWorkflowDefinitionService
    {
        Task<WorkflowDefinition> CreateDefinitionAsync(WorkflowEngine.Presentation.CreateWorkflowDefinitionRequest request);
        Task<WorkflowDefinition?> GetDefinitionAsync(string id);
        Task<List<WorkflowDefinition>> GetAllDefinitionsAsync();
    }

    public class WorkflowDefinitionService : IWorkflowDefinitionService
    {
        private readonly IPersistenceService _persistence;
        private readonly IWorkflowValidator _validator;

        public WorkflowDefinitionService(IPersistenceService persistence, IWorkflowValidator validator)
        {
            _persistence = persistence;
            _validator = validator;
        }

        public async Task<WorkflowDefinition> CreateDefinitionAsync(WorkflowEngine.Presentation.CreateWorkflowDefinitionRequest request)
        {
            var definition = new WorkflowDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                States = request.States,
                Actions = request.Actions,
                Description = request.Description
            };

            await _validator.ValidateDefinitionAsync(definition);
            await _persistence.SaveDefinitionAsync(definition);
            return definition;
        }

        public async Task<WorkflowDefinition?> GetDefinitionAsync(string id)
        {
            return await _persistence.GetDefinitionAsync(id);
        }

        public async Task<List<WorkflowDefinition>> GetAllDefinitionsAsync()
        {
            return await _persistence.GetAllDefinitionsAsync();
        }
    }

    public interface IWorkflowInstanceService
    {
        Task<WorkflowInstance> StartInstanceAsync(string definitionId);
        Task<WorkflowInstance?> GetInstanceAsync(string id);
        Task<List<WorkflowInstance>> GetAllInstancesAsync();
        Task<WorkflowInstance> ExecuteActionAsync(string instanceId, string actionId);
    }

    public class WorkflowInstanceService : IWorkflowInstanceService
    {
        private readonly IPersistenceService _persistence;
        private readonly IWorkflowDefinitionService _definitionService;
        private readonly IWorkflowValidator _validator;

        public WorkflowInstanceService(
            IPersistenceService persistence,
            IWorkflowDefinitionService definitionService,
            IWorkflowValidator validator)
        {
            _persistence = persistence;
            _definitionService = definitionService;
            _validator = validator;
        }

        public async Task<WorkflowInstance> StartInstanceAsync(string definitionId)
        {
            var definition = await _definitionService.GetDefinitionAsync(definitionId);
            if (definition == null)
                throw new ValidationException($"Workflow definition '{definitionId}' not found");

            var initialState = definition.States.First(s => s.IsInitial);
            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid().ToString(),
                DefinitionId = definitionId,
                CurrentStateId = initialState.Id
            };

            await _persistence.SaveInstanceAsync(instance);
            return instance;
        }

        public async Task<WorkflowInstance?> GetInstanceAsync(string id)
        {
            return await _persistence.GetInstanceAsync(id);
        }

        public async Task<List<WorkflowInstance>> GetAllInstancesAsync()
        {
            return await _persistence.GetAllInstancesAsync();
        }

        public async Task<WorkflowInstance> ExecuteActionAsync(string instanceId, string actionId)
        {
            var instance = await _persistence.GetInstanceAsync(instanceId);
            if (instance == null)
                throw new ValidationException($"Workflow instance '{instanceId}' not found");

            var definition = await _definitionService.GetDefinitionAsync(instance.DefinitionId);
            if (definition == null)
                throw new ValidationException($"Workflow definition '{instance.DefinitionId}' not found");

            var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
            if (action == null)
                throw new ValidationException($"Action '{actionId}' not found in workflow definition");

            await _validator.ValidateActionExecutionAsync(instance, action, definition);

            // Execute the action
            var previousStateId = instance.CurrentStateId;
            instance.CurrentStateId = action.ToState;
            
            instance.History.Add(new ActionHistory
            {
                ActionId = actionId,
                FromStateId = previousStateId,
                ToStateId = action.ToState
            });

            // Check if workflow is completed
            var currentState = definition.States.First(s => s.Id == instance.CurrentStateId);
            if (currentState.IsFinal)
            {
                instance.CompletedAt = DateTime.UtcNow;
            }

            await _persistence.SaveInstanceAsync(instance);
            return instance;
        }
    }
}
