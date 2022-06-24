namespace Mntnr.Toolkit

open System.Xml

[<RequireQualifiedAccess>]
module Xml =
  let appendNode parentXPath nodeXml (xmlFilePath:string) =
    let doc = XmlDocument()
    doc.Load(xmlFilePath)
    let refNode = doc.SelectSingleNode(parentXPath)
    let importDoc = XmlDocument()
    importDoc.LoadXml(nodeXml)
    let nodeToInsert = doc.ImportNode(importDoc.DocumentElement, true)
    refNode.AppendChild nodeToInsert |> ignore
    doc.Save(xmlFilePath)

  let replaceNodeText xPath content (xmlFilePath:string) =
    let doc = XmlDocument()
    doc.Load(xmlFilePath)
    let node = doc.SelectSingleNode(xPath)
    node.InnerText <- content
    doc.Save(xmlFilePath)

  let removeNode xPath (xmlFilePath:string) =
    let doc = XmlDocument()
    doc.Load(xmlFilePath)
    let node = doc.SelectSingleNode(xPath)
    node.ParentNode.RemoveChild(node) |> ignore
    doc.Save(xmlFilePath)
