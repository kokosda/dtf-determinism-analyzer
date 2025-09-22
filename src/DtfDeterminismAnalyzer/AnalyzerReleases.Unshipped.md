; Unshipped analyzer releases
; Format: Rule ID | Category | Severity | Notes

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DFA0001 | DTF.Determinism | Error | Do not use DateTime.Now/UtcNow/Stopwatch in orchestrators
DFA0002 | DTF.Determinism | Error | Do not use Guid.NewGuid() in orchestrators  
DFA0003 | DTF.Determinism | Error | Do not use Random without fixed seed
DFA0004 | DTF.Determinism | Error | Do not perform I/O or network calls in orchestrators
DFA0005 | DTF.Determinism | Error | Do not read environment variables in orchestrators
DFA0006 | DTF.Determinism | Error | Do not use static mutable state in orchestrators
DFA0007 | DTF.Determinism | Error | Do not block threads in orchestrators
DFA0008 | DTF.Determinism | Error | Do not start non-durable async operations
DFA0009 | DTF.Determinism | Error | Avoid .NET threading APIs like ConfigureAwait(false)
DFA0010 | DTF.Determinism | Error | Do not use bindings inside orchestrators