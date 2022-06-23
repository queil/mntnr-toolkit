namespace Mntnr.Tests

open Mntnr.Toolkit
open System.IO
open Expecto

module Toolkit =

  [<Tests>]
  let tests =
    testList "Toolkit" [

      test "Should create file" {
        let fileName = "new-file.tmp"
        File.create fileName ["Content"]
        $"File '{fileName}' should be created" |> Expect.equal (File.Exists fileName) true
        let lines = File.readLines fileName
        $"File content should be equal to 'Content'" |> Expect.equal (lines |> Seq.toList) ["Content"]
      }

      test "Should delete file" {
        let fileName = "delete-it.tmp"
        File.create fileName ["Content"]
        $"File '{fileName}' should exist before attempting deletion" |> Expect.equal (File.Exists fileName) true
        File.delete fileName
        $"File {fileName} should be deleted" |> Expect.equal (File.Exists fileName) false
      }

      test "Should replace file" {
        let fileName = "Test.Dockerfile.tmp"
        File.create fileName [
          "FROM my.registry/my/image:0.14.0-alpine"
          "# A COMMENT"
          "RUN ls -la"
          """ENTRYPOINT ["/app/test-9939-3232"]"""
        ]

        File.replace fileName (fun lines ->
                {|
                    projName = lines |> Seq.pick(function 
                    | ParseRegex """ENTRYPOINT \[\"\/app\/(.*)\"\]""" [name] -> Some name 
                    | _ -> None)
                |}
              ) (fun vars -> [
                  $"FROM my.registry/images/{vars.projName}:9.9.9"
                  $"""ENTRYPOINT ["{vars.projName}"]"""
              ])
        $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          "FROM my.registry/images/test-9939-3232:9.9.9"
          """ENTRYPOINT ["test-9939-3232"]"""
        ]
      }

      test "Should append XML node" {
        let fileName = "test.csproj.tmp"
        File.create fileName [
          """<Project Sdk="Microsoft.NET.Sdk.Web">"""
          """<PropertyGroup>"""
          """</PropertyGroup>"""
          """</Project>"""
        ]
        Xml.append "/Project/PropertyGroup[1]" "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>" fileName
        $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          """<Project Sdk="Microsoft.NET.Sdk.Web">"""
          """  <PropertyGroup>"""
          """    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"""
          """  </PropertyGroup>"""
          """</Project>"""
        ]
      }

      test "Should add csproj property" {
        let fileDir = "test-files/csproj"
        let fileName = $"{fileDir}/test.csproj"
        File.create fileName [
          """<Project Sdk="Microsoft.NET.Sdk.Web">"""
          """<PropertyGroup>"""
          """</PropertyGroup>"""
          """</Project>"""
        ]
        Csproj.addProperty "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>" fileDir
        $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          """<Project Sdk="Microsoft.NET.Sdk.Web">"""
          """  <PropertyGroup>"""
          """    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"""
          """  </PropertyGroup>"""
          """</Project>"""
        ]
      }

      test "Should stream to file" {
       let fileName = "stream-to.txt.tmp"

       ["pear"; "why not"] |> File.streamTo fileName (function | ParseRegex "p(ear)" [ear] -> $"b{ear}" | s -> s)

       $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          "bear"
          "why not"
        ]
      }

      test "Should stream file" {
       let fileName = "stream.txt.tmp"
       ["pear"; "why not"] |> File.create fileName
       File.stream fileName (function | ParseRegex "p(ear)" [ear] -> $"b{ear}" | s -> s)
       
       $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          "bear"
          "why not"
        ]
      }

      test "Should append after line with regex captures" {
        let fileName = "insertAfter.txt.tmp"
        ["pear blue"; "why not"] |> File.create fileName
        File.stream fileName <| File.insertAfter "^pear (.*)$" (fun [colour] ->
            [
              $"bear {colour}"
              "static"
            ])

        $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          "pear blue"
          "bear blue"
          "static"
          "why not"
        ]
      }

      test "Should append after line simple" {
        let fileName = "insertLineAfter.txt.tmp"
        ["pear blue"; "why not"; "pear blue"] |> File.create fileName
        File.insertLineAfter "pear blue" "static" |>
          File.stream fileName 

        $"File should have correct content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
          "pear blue"
          "static"
          "why not"
          "pear blue"
          "static"
        ]
      }


      test "Should replace XML node contents" {
        let fileName = "nodeContentsTest.csproj.tmp"
        File.create fileName [
          """<Project Sdk="Microsoft.NET.Sdk.Web">"""
          """<PropertyGroup>"""
          """<TargetFramework>netcoreapp3.1</TargetFramework>"""
          """</PropertyGroup>"""
          """</Project>"""
        ]
        Xml.replace "/Project/PropertyGroup/TargetFramework" "net5.0" fileName
        $"File should have updated content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
         """<Project Sdk="Microsoft.NET.Sdk.Web">"""
         """  <PropertyGroup>"""
         """    <TargetFramework>net5.0</TargetFramework>"""
         """  </PropertyGroup>"""
         """</Project>"""
        ]
      }

      test "Should change csproj target framework property" {
        let fileDir = "test-files/csproj-tfm"
        let fileName = $"{fileDir}/test.csproj"
        File.create fileName [
          """<Project Sdk="Microsoft.NET.Sdk.Web">"""
          """<PropertyGroup>"""
          """<TargetFramework>netcoreapp3.1</TargetFramework>"""
          """</PropertyGroup>"""
          """</Project>"""
        ]
        Csproj.changeTfm "net5.0" fileDir
        $"File should have updated content" |> Expect.equal (fileName |> File.readLines |> Seq.toList) [
            """<Project Sdk="Microsoft.NET.Sdk.Web">"""
            """  <PropertyGroup>"""
            """    <TargetFramework>net5.0</TargetFramework>"""
            """  </PropertyGroup>"""
            """</Project>"""
           ]
      }


      test "Should get git remote url" {

         "Unexpected remote url" |> Expect.equal ( Git.relativePath "git@my.git.repo:queil/mntnr-toolkit.git") "queil/mntnr-toolkit"
         "Unexpected remote url" |> Expect.equal ( Git.relativePath "https://git-token:fake-token-value@my.git.repo/queil/mntnr-toolkit.git") "queil/mntnr-toolkit"
       }

      test "Should match remote path" {

         "Unexpected remote url" |> Expect.equal ( Git.remotePathMatches "^queil\/mntnr-toolkit") true
         "Unexpected remote url" |> Expect.equal ( Git.remotePathMatches "^dev\/something") false
       }
    ]
