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
SF1016 | Sailfish.Usage | Warning | Variable-dependent state built in a once-per-class hook (GlobalSetup/GlobalTeardown) is silently frozen; build it in MethodSetup
SF1020 | Sailfish.Usage | Error | Sailfish lifecycle methods must be public
SF1021 | Sailfish.Usage | Error | Only one Sailfish lifecycle attribute is allowed per method
SF1022 | Sailfish.Usage | Error | A method may not be decorated with both [SailfishMethod] and [Trawl]
SF1023 | Sailfish.Usage | Warning | A [Trawl] load scenario writes to mutable shared-instance state without synchronization
SF7000 | Sailfish.Suppression | Hidden | Suppresses warnings when a non nullable property is set in the global setup method
SF1300 | Sailfish.Usage | Error | IsBaseline=true on a method that isn't in any comparison group
SF1301 | Sailfish.Usage | Error | At most one method per comparison group (explicit or implicit) may set IsBaseline=true
SF1302 | Sailfish.Usage | Warning | A comparison group (explicit or implicit) must contain at least two methods


