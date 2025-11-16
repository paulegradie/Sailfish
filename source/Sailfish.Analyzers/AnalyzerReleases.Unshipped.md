; Unshipped analyzer rules will appear here.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SF1001 | Sailfish.Performance | Warning | Unused return value inside SailfishMethod
SF1002 | Sailfish.Performance | Warning | Constant-only computation in SailfishMethod
SF1003 | Sailfish.Performance | Warning | Empty loop body inside SailfishMethod
SF1010 | Sailfish.Usage | Error | Properties decorated with the SailfishVariableAttribute must be public
SF1011 | Sailfish.Usage | Error | Properties decorated with the SailfishVariableAttribute must have public getters
SF1012 | Sailfish.Usage | Error | Properties decorated with the SailfishVariableAttribute must have public setters
SF1013 | Sailfish.Usage | Error | Properties initialized in the global setup must be public
SF1014 | Sailfish.Usage | Error | Properties assigned in the global setup must have public getters
SF1015 | Sailfish.Usage | Error | Properties assigned in the global setup must have public setters
SF1020 | Sailfish.Usage | Error | Sailfish lifecycle methods must be public
SF1021 | Sailfish.Usage | Error | Only one Sailfish lifecycle attribute is allowed per method
SF7000 | Sailfish.Suppression | Hidden | Suppresses warnings when a non nullable property is set in the global setup method


