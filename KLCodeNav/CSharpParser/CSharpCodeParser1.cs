// Decompiled with JetBrains decompiler
// Type: RazorEngine.Compilation.CSharp.CSharpCodeParser
// Assembly: RazorEngine, Version=3.10.0.0, Culture=neutral, PublicKeyToken=9ee697374c7e744a
// MVID: 702D8398-0A63-49E5-82D9-D2B5999EA1ED
// Assembly location: C:\Users\klockerby\Downloads\razorengine.3.10.0\lib\net45\RazorEngine.dll

using RazorEngine.CodeGenerators;
using System;
using System.Security;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Text;
using System.Web.Razor.Tokenizer;
using System.Web.Razor.Tokenizer.Symbols;

namespace RazorEngine.Compilation.CSharp
{
  /// <summary>Defines a code parser that supports the C# syntax.</summary>
  [SecurityCritical]
  public class CSharpCodeParser : CSharpCodeParser
  {
    private SourceLocation? _endInheritsLocation;
    private bool _modelStatementFound;

    /// <summary>
    /// Initialises a new instance of <see cref="T:RazorEngine.Compilation.CSharp.CSharpCodeParser" />.
    /// </summary>
    public CSharpCodeParser() => this.MapDirectives(new Action(this.ModelDirective), new string[1]
    {
      "model"
    });

    /// <summary>Parses the inherits statement.</summary>
    [SecurityCritical]
    protected virtual void InheritsDirective()
    {
      ((TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>) this).AcceptAndMoveNext();
      this._endInheritsLocation = new SourceLocation?(((TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>) this).CurrentLocation);
      this.InheritsDirectiveCore();
      this.CheckForInheritsAndModelStatements();
    }

    private void CheckForInheritsAndModelStatements()
    {
      if (!this._modelStatementFound || !this._endInheritsLocation.HasValue)
        return;
      ((ParserBase) this).Context.OnError(this._endInheritsLocation.Value, "The 'inherits' keyword is not allowed when a 'model' keyword is used.");
    }

    /// <summary>Parses the model statement.</summary>
    [SecurityCritical]
    protected virtual void ModelDirective()
    {
      ((TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>) this).AcceptAndMoveNext();
      SourceLocation currentLocation = ((TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>) this).CurrentLocation;
      this.BaseTypeDirective("The 'model' keyword must be followed by a type name on the same line.", new Func<string, SpanCodeGenerator>(this.CreateModelCodeGenerator));
      if (this._modelStatementFound)
        ((ParserBase) this).Context.OnError(currentLocation, "Only one 'model' statement is allowed in a file.");
      this._modelStatementFound = true;
      this.CheckForInheritsAndModelStatements();
    }

    [SecurityCritical]
    private SpanCodeGenerator CreateModelCodeGenerator(string model) => (SpanCodeGenerator) new SetModelTypeCodeGenerator(model, (Func<Type, string, string>) ((templateType, modelTypeName) => CompilerServicesUtility.CSharpCreateGenericType(templateType, modelTypeName, true)));
  }
}
