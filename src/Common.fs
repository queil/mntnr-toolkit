namespace Mntnr.Toolkit

open System.IO
open System.Text.RegularExpressions
open Fake.Core

[<AutoOpen>]
module Common =

    let (|ParseRegex|_|) regex str =
        let m = Regex(regex).Match(str)

        if m.Success then
            Some(List.tail [ for x in m.Groups -> x.Value ])
        else
            None

    /// Creates a new directory with intermediate path if does not exist
    let mkdir (path: string) =
        Directory.CreateDirectory path |> ignore

    let private createSh workingDir script =
        RawCommand("sh", Arguments.OfArgs [ "-c"; script ])
        |> CreateProcess.fromCommand
        |> CreateProcess.ensureExitCode
        |> CreateProcess.withWorkingDirectory workingDir

    /// Run an arbitrary shell script
    let sh script =
        let run workingDir script =
            createSh workingDir script |> Proc.run |> ignore

        run "." script

    /// Run an arbitrary shell script and returns stdout
    let shValue script =
        let run workingDir script =
            createSh workingDir script
            |> CreateProcess.redirectOutput
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> fun x -> x.Result.Output.Trim()

        run "." script
