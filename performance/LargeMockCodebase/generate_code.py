#!/usr/bin/env python3
"""
Performance test code generator for DTF Determinism Analyzer.
Generates large amounts of orchestrator and activity code to test analyzer performance.
"""

import os
import random
from typing import List

def generate_orchestrator_violations() -> List[str]:
    """Generate various analyzer violation patterns"""
    violations = [
        "DateTime.Now",
        "DateTime.UtcNow", 
        "Guid.NewGuid()",
        "new Random().Next()",
        "Environment.GetEnvironmentVariable(\"TEST\")",
        "Thread.Sleep(1000)",
        "Task.Delay(1000)",
        "File.ReadAllText(\"test.txt\")",
        "HttpClient.GetAsync(\"https://example.com\")",
        "Task.Run(() => {})",
    ]
    return random.sample(violations, k=random.randint(1, 4))

def generate_orchestrator(namespace: str, class_name: str, method_count: int = 5) -> str:
    """Generate an orchestrator class with multiple methods"""
    
    methods = []
    for i in range(method_count):
        violations = generate_orchestrator_violations()
        violation_code = "\n        ".join([f"var temp{j} = {v};" for j, v in enumerate(violations)])
        
        method = f"""
    [FunctionName("{class_name}_Method{i}")]
    public async Task<string> Method{i}Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {{
        // Performance test method with violations
        {violation_code}
        
        // Call some activities
        await context.CallActivityAsync<string>("Activity{i % 10}", "data");
        await context.CallActivityAsync<int>("CalculateActivity", i);
        
        return "completed";
    }}"""
        methods.append(method)
    
    return f"""
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace {namespace}
{{
    public class {class_name}
    {{{''.join(methods)}
    }}
}}
"""

def generate_activity(namespace: str, class_name: str, method_count: int = 3) -> str:
    """Generate an activity class with multiple methods"""
    
    methods = []
    for i in range(method_count):
        method = f"""
    [FunctionName("{class_name}_Activity{i}")]
    public async Task<string> Activity{i}Async([ActivityTrigger] string input, ILogger logger)
    {{
        logger.LogInformation($"Processing {{input}} in activity {i}");
        
        // Simulate work
        await Task.Delay(Random.Shared.Next(10, 100));
        
        // Activities can use non-deterministic operations
        var timestamp = DateTime.Now;
        var guid = Guid.NewGuid();
        var env = Environment.GetEnvironmentVariable("PATH");
        
        return $"Activity{i} result: {{input}}-{{timestamp}}-{{guid}}";
    }}"""
        methods.append(method)
    
    return f"""
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace {namespace}
{{
    public class {class_name}
    {{{''.join(methods)}
    }}
}}
"""

def generate_dtf_orchestrator(namespace: str, class_name: str, method_count: int = 5) -> str:
    """Generate DTF core orchestrator class"""
    
    methods = []
    for i in range(method_count):
        violations = generate_orchestrator_violations()
        violation_code = "\n        ".join([f"var temp{j} = {v};" for j, v in enumerate(violations)])
        
        method = f"""
    public async Task<string> Method{i}Async(TaskOrchestrationContext context)
    {{
        // DTF performance test method with violations
        {violation_code}
        
        // Call some activities
        await context.CallActivityAsync<string>("DtfActivity{i % 10}", "data");
        await context.CallActivityAsync<int>("DtfCalculateActivity", i);
        
        return "dtf completed";
    }}"""
        methods.append(method)
    
    return f"""
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DurableTask;

namespace {namespace}
{{
    public class {class_name}
    {{{''.join(methods)}
    }}
}}
"""

def main():
    """Generate large codebase for performance testing"""
    
    output_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Generate 50 Azure Functions orchestrator classes
    for i in range(50):
        namespace = f"Performance.Azure.Batch{i // 10}"
        class_name = f"TestOrchestrator{i:03d}"
        content = generate_orchestrator(namespace, class_name, method_count=8)
        
        filename = os.path.join(output_dir, f"Azure_Orchestrator_{i:03d}.cs")
        with open(filename, 'w') as f:
            f.write(content)
    
    # Generate 30 Azure Functions activity classes  
    for i in range(30):
        namespace = f"Performance.Azure.Activities.Batch{i // 10}"
        class_name = f"TestActivity{i:03d}"
        content = generate_activity(namespace, class_name, method_count=5)
        
        filename = os.path.join(output_dir, f"Azure_Activity_{i:03d}.cs")
        with open(filename, 'w') as f:
            f.write(content)
    
    # Generate 25 DTF core orchestrator classes
    for i in range(25):
        namespace = f"Performance.DTF.Batch{i // 10}"
        class_name = f"DtfOrchestrator{i:03d}"
        content = generate_dtf_orchestrator(namespace, class_name, method_count=6)
        
        filename = os.path.join(output_dir, f"DTF_Orchestrator_{i:03d}.cs")
        with open(filename, 'w') as f:
            f.write(content)
    
    # Generate summary
    total_files = 50 + 30 + 25
    total_methods = (50 * 8) + (30 * 5) + (25 * 6)
    
    summary = f"""
// PERFORMANCE TEST CODEBASE SUMMARY
// Generated {total_files} files with {total_methods} total methods
//
// Azure Functions Orchestrators: 50 classes × 8 methods = 400 orchestrator methods
// Azure Functions Activities: 30 classes × 5 methods = 150 activity methods  
// DTF Core Orchestrators: 25 classes × 6 methods = 150 orchestrator methods
//
// Total orchestrator methods: 550 (should trigger analyzer rules)
// Total activity methods: 150 (should not trigger analyzer rules)
// Expected analyzer violations: ~2200-2750 (4-5 violations per orchestrator method)
"""
    
    with open(os.path.join(output_dir, "CodebaseSummary.txt"), 'w') as f:
        f.write(summary)
    
    print(f"Generated {total_files} files with {total_methods} total methods")
    print(f"Expected ~2200-2750 analyzer violations to process")

if __name__ == "__main__":
    main()