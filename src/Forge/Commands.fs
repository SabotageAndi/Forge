﻿module Forge.Commands

open System
open System.Text
open Argu


/// Custom Command Line Argument
type CLIArg = CustomCommandLineAttribute
/// Alternative Command Line Argument
type CLIAlt = AltCommandLineAttribute

type Command =
    | [<First>][<CLIArg "new">][<CLIAlt "-n">] 
        New
    | [<First>][<CLIArg "file">][<CLIAlt "-f">] 
        File
    | [<First>][<CLIArg "reference">][<CLIAlt "-ref">] 
        Reference
    | [<First>][<CLIArg "update">] 
        Update
    | [<First>][<CLIArg "paket">] 
        Paket
    | [<First>][<CLIArg "fake">] 
        Fake
    | [<First>][<CLIArg "refresh">] 
        Refresh
    | [<First>][<CLIArg "help">] 
        Help
    | [<First>][<CLIArg "exit">][<CLIAlt("quit","-q")>] 
        Exit
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | New -> "Create new project"
            | File -> "Adds or removes file from current folder and project."
            | Reference -> "Adds or removes reference from current project."
            | Update -> "Updates Paket or FAKE"
            | Paket -> "Runs Paket"
            | Fake -> "Runs FAKE"
            | Refresh -> "Refreshes the template cache"
            | Help -> "Displays help"
            | Exit -> "Exits interactive mode"
    member this.Name =
        let uci,_ = Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(this, typeof<Command>)
        (uci.GetCustomAttributes(typeof<CustomCommandLineAttribute>)
        |> Seq.head
        :?> CustomCommandLineAttribute).Name

type NewArgs =
    | [<CLIAlt "-a">] Name of string
    | [<CLIAlt "-d">] Dir of string
    | [<CLIAlt "-t">] Template of string
    | No_Paket
    | No_Fake
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Name _ -> "Project name"
            | Dir _ -> "Project directory, relative to Forge working directory"
            | Template _ -> "Template name"
            | No_Paket -> "Don't use Paket for dependency managment"
            | No_Fake -> "Don't use FAKE for build"

type FileArgs =
    |[<CLIArg "add">][<CLIAlt "-a">] Add of string
    |[<CLIArg "remove">][<CLIAlt "-r">] Remove of string
    |[<CLIArg "list">][<CLIAlt "-l">] List
    |[<CLIArg "order">][<CLIAlt "-o">] Order of string * string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Add _ -> "Adds a file to the current folder and project"
            | Remove _ -> "Removes the file from disk and the project"
            | List  -> "List all files"
            | Order _ -> "Moves file1 immediately before file2 in the project"

type ReferenceArgs =
    |[<CLIArg "add">][<CLIAlt "-a">] Add of string
    |[<CLIArg "remove">][<CLIAlt "-r">] Remove of string
    |[<CLIArg "list">][<CLIAlt "-l">] List
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Add _ -> "Adds a reference to the current project"
            | Remove _ -> "Removes the reference from the project"
            | List  -> " List all references"

type UpdateArgs =
    |[<CLIArg "paket">] Paket
    |[<CLIArg "fake">] Fake
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Paket -> "Updates Paket to latest version"
            | Fake _ -> "Updates FAKE to latest version"

let processCommand<'T when 'T :> IArgParserTemplate> (commandF : ParseResults<'T> -> int) _ args =
    let parser = ArgumentParser.Create<'T>()
    let results =
        parser.Parse
            (inputs = args, raiseOnUsage = false, ignoreMissing = true,
             errorHandler = ProcessExiter())
    if results.IsUsageRequested then
        parser.Usage("Available parameters:") |> System.Console.WriteLine
        0
    else
        commandF results

let project (results : ParseResults<_>) =
    let projectName = defaultArg (results.TryGetResult <@ NewArgs.Name @>) ""
    let projectDir = defaultArg (results.TryGetResult <@ NewArgs.Dir @>) ""
    let templateName = defaultArg (results.TryGetResult <@ NewArgs.Template @>) ""
    let paket = not ^ results.Contains <@ NewArgs.No_Paket @>
    let fake = not ^ results.Contains <@ NewArgs.No_Fake @>
    Project.New projectName projectDir templateName paket fake
    1

let file (results : ParseResults<_>) =
    let add = results.TryGetResult <@ FileArgs.Add @>
    let remove = results.TryGetResult <@ FileArgs.Remove @>
    let list = results.Contains <@ FileArgs.List @>
    let order = results.TryGetResult <@ FileArgs.Order @>
    match add, remove, list, order with
    | Some fn, _, _, _ -> Files.Add fn
    | _, Some fn, _, _ -> Files.Remove fn
    | _, _, true, _ -> Files.List ()
    | _, _, _, Some (f1,f2) -> Files.Order f1 f2
    | None, None, false, None -> ()
    0

let reference (results : ParseResults<_>) =
    let add = results.TryGetResult <@ ReferenceArgs.Add @>
    let remove = results.TryGetResult <@ ReferenceArgs.Remove @>
    let list = results.Contains <@ ReferenceArgs.List @>
    match add, remove, list with
    | Some fn, _, _ -> Files.Add fn
    | _, Some fn, _ -> Files.Remove fn
    | _, _, true -> Files.List ()
    | None, None, false -> ()
    0

let update (results : ParseResults<_>) =
    let paket = results.Contains <@ UpdateArgs.Paket @>
    let fake = results.Contains <@ UpdateArgs.Fake @>
    match paket,fake with
    | true, _ -> Paket.Update ()
    | _, true -> Fake.Update ()
    | false, false -> ()
    0




