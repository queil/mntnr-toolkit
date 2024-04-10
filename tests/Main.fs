module tests

open Expecto
open System

Tests.runTestsInAssemblyWithCLIArgs [] (Environment.GetCommandLineArgs()[1..])
|> ignore
