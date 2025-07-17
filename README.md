
<img width="1287" height="278" alt="Screenshot 2025-07-18 021048" src="https://github.com/user-attachments/assets/73c44f9a-d1a3-451a-8d34-289a740da2e6" />



<img width="590" height="233" alt="Screenshot 2025-07-18 021103" src="https://github.com/user-attachments/assets/701db7b1-da5c-4fe0-bf6c-dd83364758e4" />



<img width="645" height="504" alt="Screenshot 2025-07-18 021217" src="https://github.com/user-attachments/assets/9467f4a9-bbcc-4e57-a4e0-f0890aa78a51" />



 Infonetica Workflow Engine

This project is a minimal state-machine–based backend built with **.NET 8** and ASP.NET Core Minimal APIs. It allows clients to define configurable workflows, instantiate them, execute valid transitions (actions), and query current states—all with full validation and file-based persistence.

 Functional Overview

This engine supports:

- Defining workflows with **states** and **actions**
- Starting workflow **instances** based on definitions
- Executing **actions** to transition between states
- Viewing the current state and **action history**
- Rejecting invalid definitions or transitions

 Core Concepts

| Concept            | Description                                                                 |
|--------------------|-----------------------------------------------------------------------------|
| `State`            | Contains `id`, `name`, and flags like `isInitial`, `isFinal`, `enabled`     |
| `Action`           | Contains `id`, `name`, `enabled`, `fromStates[]`, `toState`                 |
| `WorkflowDefinition` | Contains states and actions. Must have exactly one initial state          |
| `WorkflowInstance` | Represents an active workflow run, stores current state and transition log  |

 Getting Started

 Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download)

Running the Project

```bash
git clone https://github.com/Adithya445/Infonetica_workflow_engine.git
cd Infonetica_workflow_engine
dotnet run


