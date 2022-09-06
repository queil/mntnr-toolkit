namespace Mntnr.Toolkit

open System.Xml

[<RequireQualifiedAccess>]
module Xml =

  open System.IO
  let private ensureFinalEol file =
    use stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)
    stream.Seek(-1, SeekOrigin.End) |> ignore
    if stream.ReadByte() <> 10 then stream.WriteByte(10uy)
    stream.Flush()
    
  type XmlDocument with
    member x.SaveWithFinalEol (path: string) =
       x.Save(path)
       ensureFinalEol path
    
  let appendNode parentXPath nodeXml (xmlFilePath:string) =
    let doc = XmlDocument()
    doc.Load(xmlFilePath)
    let refNode = doc.SelectSingleNode(parentXPath)
    let importDoc = XmlDocument()
    importDoc.LoadXml(nodeXml)
    let nodeToInsert = doc.ImportNode(importDoc.DocumentElement, true)
    refNode.AppendChild nodeToInsert |> ignore
    doc.SaveWithFinalEol(xmlFilePath)

  let replaceNodeText xPath mapContent (xmlFilePath:string) =
    let doc = XmlDocument()
    doc.Load(xmlFilePath)
    let node = doc.SelectSingleNode(xPath)
    node.InnerText <- mapContent node.InnerText
    doc.SaveWithFinalEol(xmlFilePath)

  let removeNode xPath (xmlFilePath:string) =
    let doc = XmlDocument()
    doc.Load(xmlFilePath)
    let node = doc.SelectSingleNode(xPath)
    node.ParentNode.RemoveChild(node) |> ignore
    doc.SaveWithFinalEol(xmlFilePath)
