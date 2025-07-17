using System.Text.Json;
using WorkflowEngine.DomainModels;

namespace WorkflowEngine.DataAccess
{
    public interface IPersistenceService
    {
        Task SaveDefinitionAsync(WorkflowDefinition definition);
        Task<WorkflowDefinition?> GetDefinitionAsync(string id);
        Task<List<WorkflowDefinition>> GetAllDefinitionsAsync();
        Task SaveInstanceAsync(WorkflowInstance instance);
        Task<WorkflowInstance?> GetInstanceAsync(string id);
        Task<List<WorkflowInstance>> GetAllInstancesAsync();
    }

    public class FilePersistenceService : IPersistenceService
    {
        private readonly string _dataDirectory = "data";
        private readonly string _definitionsFile = "definitions.json";
        private readonly string _instancesFile = "instances.json";
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public FilePersistenceService()
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        public async Task SaveDefinitionAsync(WorkflowDefinition definition)
        {
            var definitions = await GetAllDefinitionsAsync();
            definitions.RemoveAll(d => d.Id == definition.Id);
            definitions.Add(definition);
            await SaveDefinitionsAsync(definitions);
        }

        public async Task<WorkflowDefinition?> GetDefinitionAsync(string id)
        {
            var definitions = await GetAllDefinitionsAsync();
            return definitions.FirstOrDefault(d => d.Id == id);
        }

        public async Task<List<WorkflowDefinition>> GetAllDefinitionsAsync()
        {
            var filePath = Path.Combine(_dataDirectory, _definitionsFile);
            if (!File.Exists(filePath))
                return new List<WorkflowDefinition>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<WorkflowDefinition>>(json, _jsonOptions) ?? new List<WorkflowDefinition>();
        }

        public async Task SaveInstanceAsync(WorkflowInstance instance)
        {
            var instances = await GetAllInstancesAsync();
            instances.RemoveAll(i => i.Id == instance.Id);
            instances.Add(instance);
            await SaveInstancesAsync(instances);
        }

        public async Task<WorkflowInstance?> GetInstanceAsync(string id)
        {
            var instances = await GetAllInstancesAsync();
            return instances.FirstOrDefault(i => i.Id == id);
        }

        public async Task<List<WorkflowInstance>> GetAllInstancesAsync()
        {
            var filePath = Path.Combine(_dataDirectory, _instancesFile);
            if (!File.Exists(filePath))
                return new List<WorkflowInstance>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<WorkflowInstance>>(json, _jsonOptions) ?? new List<WorkflowInstance>();
        }

        private async Task SaveDefinitionsAsync(List<WorkflowDefinition> definitions)
        {
            var filePath = Path.Combine(_dataDirectory, _definitionsFile);
            var json = JsonSerializer.Serialize(definitions, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        private async Task SaveInstancesAsync(List<WorkflowInstance> instances)
        {
            var filePath = Path.Combine(_dataDirectory, _instancesFile);
            var json = JsonSerializer.Serialize(instances, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
    }
}