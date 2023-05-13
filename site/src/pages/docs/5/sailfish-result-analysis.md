---
title: Sailfish Result Analysis
---

Sailfish provides default analysis behavior to get you up and running with your tests quickly. When executing tests via the `SailfishRunner`, a default tracking directory call `performance_tracking` will be created inside the project's bin directory. Upon the successful completion of a run, a result tracking file will be deposited into this directory via the default handler that is responsible for writing out result data.

****Note**: The tracking handler is that which implements `INotificationHandler<WriteCurrentTrackingFileCommand>`. This handler can be overriden.**

Upon the successful completion of a second run, Sailfish will attempt to read the files in that tracking directory, discover the most recent runfile, and then attempt to performan a statistical analysis for each test in the current run found in the previous run. If there are new tests in the current run, they will not be included in the analysis and will therefore not appear in the result output.

Where data is written to, where data is read from, and how it is preprocessed prior to submission for analysis are all customizable.
