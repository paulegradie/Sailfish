# Sailfish Test Runner Scenario

This project demonstrates how how the Sailfish.TestAdapter allows you to execute test classes directly from a class library project that has the test adapter installed.

Since Sailfish only supports a single execution method per class, you'll only see the small play button in your IDE appear next to the class name.

Hitting the play button will call the Sailfish execution logic, pass the class to the executor, create a parameter grid based on any variables you have defined, and then invoke the execution method for each variable combination.


# WIP

This feature is a work in progress. It currently works to an extent - its currently being tested in jetbrains Rider and Visual Studio 2022. 
It doesn't work as well as it should and it is not production ready.