#if !MINIMAL

/*
*
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Text;
using Facepunch;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Carbon.Modules;

public partial class AdminModule : CarbonModule<AdminConfig, AdminData>
{
	public class SourceViewerTab : Tab
	{
		public Action<PlayerSession> Close;
		public bool Loading;

		internal const string AttrCastout =
			"[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall, MethodCodeType = System.Runtime.CompilerServices.MethodCodeType.Runtime)]";

		internal const string AttrCastout2 =
			"[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";

		public SourceViewerTab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null, string access = null) : base(id, name, plugin, onChange, access)
		{
		}

		public static SourceViewerTab Make(string fileName, string content, string context, int size = 8)
		{
			var tab = new SourceViewerTab("testchat", "TestChat", Community.Runtime.CorePlugin);
			tab.OnChange += (_, tab1) =>
			{
				tab.Loading = false;
				tab1.AddColumn(0, true);
			};
			tab.Over += (_, cui, container, panel, ap) =>
			{
				var blur = cui.CreatePanel(container, panel, "0.1 0.1 0.1 0.8", blur: true);

				using var lines = TemporaryArray<string>.New(content.Split('\n'));
				var temp = Pool.GetList<string>();

				var resultContent = lines.Array.ToString("\n");

				for (int i = 0; i < lines.Length; i++) temp.Add($"{i + 1}");

				cui.CreateText(container, blur, "0.3 0.7 0.9 0.5",
					string.Join("\n", temp), size,
					align: TextAnchor.UpperRight, font: CUI.Handler.FontTypes.DroidSansMono,
					xMin: 0f, xMax: 0.02f, yMin: 0.2f, yMax: 0.95f);

				cui.CreateImage(container, blur, "fade", Cache.CUI.WhiteColor, yMin: 0.96f);
				cui.CreatePanel(container, blur, "0.2 0.2 0.2 1", xMin: 0.0275f, xMax: 0.0275f, OxMax: 1f);
				cui.CreatePanel(container, blur, "0.2 0.2 0.2 1", yMin: 0.96f, yMax: 0.96f, OyMax: 1f);

				cui.CreateText(container, blur, "0.8 0.8 0.8 1",
					$"{fileName} <color=orange>*</color>", 8,
					align: TextAnchor.UpperLeft, font: CUI.Handler.FontTypes.RobotoCondensedRegular,
					xMin: 0.036f, xMax: 1f, yMin: 0.2f, yMax: 0.9875f);

				cui.CreateText(container, blur, "0.8 0.8 0.8 0.5",
					context, 8,
					align: TextAnchor.UpperRight, font: CUI.Handler.FontTypes.RobotoCondensedRegular,
					xMin: 0.036f, xMax: 0.97f, yMin: 0.2f, yMax: 0.9875f);

				var exit = cui.CreateProtectedButton(container, blur, "0.9 0.2 0.1 1", Cache.CUI.BlankColor,
					string.Empty, 0,
					xMin: 0.978f, xMax: 1f, yMin: 0.96f, command: "adminmodule.profilerpreviewclose");
				cui.CreateImage(container, exit, "close", "1 1 1 0.8", xMin: 0.2f, xMax: 0.8f, yMin: 0.2f, yMax: 0.8f);

				if (tab.Loading)
				{
					cui.CreateText(container, blur, "0.8 0.8 0.8 1",
						"Loading, please wait..."
							.Replace("\r", "")
							.Replace("\"", "'")
							.Replace("\t", "<color=#454545>————</color>"), size,
						align: TextAnchor.UpperLeft, font: CUI.Handler.FontTypes.DroidSansMono,
						xMin: 0.036f, xMax: 1f, yMin: 0.2f, yMax: 0.95f);
				}
				else
				{
					cui.CreateText(container, blur, "0.8 0.8 0.8 1",
						resultContent
							.Replace("\r", "")
							.Replace("\"", "'")
							.Replace("\t", "<color=#454545>————</color>"), size,
						align: TextAnchor.UpperLeft, font: CUI.Handler.FontTypes.DroidSansMono,
						xMin: 0.036f, xMax: 2f, yMin: 0.2f, yMax: 0.95f);
				}

				Pool.FreeList(ref temp);
			};

			return tab;
		}
		public static SourceViewerTab MakeMethod(string assembly, string type, string method, int size = 8)
		{
			var code = SourceCodeBank.Parse(assembly);
			var codeResult = code.ParseMethod(type, method)
				.Replace(AttrCastout, string.Empty)
				.Replace(AttrCastout2, string.Empty).Trim();
			return Make($"<color=#878787>{type}.</color>{method}<color=#878787>.cs</color>",
				ProcessSyntaxHighlight(codeResult), $"{Path.GetFileNameWithoutExtension(assembly)}.dll", size);
		}
		public unsafe static SourceViewerTab MakeMethod(MonoProfiler.CallRecord call, int size = 8)
		{
			var assemblyName = MonoProfiler.AssemblyMap[call.assembly_handle];
			var code = SourceCodeBank.Parse(assemblyName, call.assembly_handle);
			var codeResult = code.ParseMethod(call.method_handle, out var type)
				.Replace(AttrCastout, string.Empty)
				.Replace(AttrCastout2, string.Empty).Trim();
			return Make(
				$"<color=#878787>{type}.</color>{call.method_name.Replace($"{type}::", string.Empty)}<color=#878787>.cs</color>",
				ProcessSyntaxHighlight(codeResult), $"{assemblyName}.dll", size);
		}

		public static string ProcessSyntaxHighlight(string content)
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(content, Encoding.UTF8),
				options: new CSharpParseOptions()
					.WithDocumentationMode(DocumentationMode.Parse)
					.WithKind(SourceCodeKind.Script)
					.WithLanguageVersion(LanguageVersion.Preview));

			// Syntax highlighting configuration
			var syntaxHighlighter = new SyntaxHighlighter();
			syntaxHighlighter.AddPattern(SyntaxKind.UsingDirective, "#0000FF");
			syntaxHighlighter.AddPattern(SyntaxKind.IfStatement, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.ElseKeyword, "#dcdcaa");
			syntaxHighlighter.AddPattern(SyntaxKind.IdentifierName, "#e0e0e0");
			syntaxHighlighter.AddPattern(SyntaxKind.PredefinedType, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.OpenBracketToken, "#b3b1b1");
			syntaxHighlighter.AddPattern(SyntaxKind.CloseBracketToken, "#b3b1b1");
			syntaxHighlighter.AddPattern(SyntaxKind.PublicKeyword, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.PrivateKeyword, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.AttributeList, "#b3b1b1");
			syntaxHighlighter.AddPattern(SyntaxKind.MethodKeyword, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.MethodDeclaration, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.IdentifierToken, "#e0e0e0");
			syntaxHighlighter.AddPattern(SyntaxKind.StaticKeyword, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.ReturnStatement, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.NamespaceDeclaration, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.UsingKeyword, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.VoidKeyword, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.ThisExpression, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.StringLiteralToken, "#d69d85");
			syntaxHighlighter.AddPattern(SyntaxKind.VariableDeclarator, "#b3b1b1");
			syntaxHighlighter.AddPattern(SyntaxKind.TrueLiteralExpression, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.FalseLiteralExpression, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.ForEachStatement, "#caf553");
			syntaxHighlighter.AddPattern(SyntaxKind.NumericLiteralExpression, "#caf553");

			return syntaxHighlighter.Process(syntaxTree);
		}

		public class SyntaxHighlighter
		{
			public void AddPattern(SyntaxKind syntaxKind, string color)
			{
				_resolver[syntaxKind] = $"<color={color}>";
			}

			public string Process(SyntaxTree syntaxTree)
			{
				var root = syntaxTree.GetRoot();
				var builder = new StringBuilder();

				WriteNode(root, builder);

				return builder.ToString();
			}

			private void WriteNode(SyntaxNode node, StringBuilder builder)
			{
				foreach (var token in node.DescendantTokens())
				{
					var text = token.ToFullString();
					var kind = token.Parent.Kind();
					var styleTag = _resolver[kind];

					if (!string.IsNullOrEmpty(styleTag))
					{
						builder.Append($"{styleTag}{text}</color>");
					}
					else
					{
						builder.Append($"{text}");
					}
				}
			}

			private readonly StyleResolver _resolver = new();
		}

		public class StyleResolver
		{
			private static readonly string[] _names = Enum.GetNames(typeof(SyntaxKind));
			private static readonly string[] _styles = new string[_names.Length];

			public string this[SyntaxKind syntaxKind]
			{
				get => _styles[_names.IndexOf(syntaxKind.ToString())];
				set => _styles[_names.IndexOf(syntaxKind.ToString())] = value;
			}
		}
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.profilerpreviewclose")]
	private void ProfilerPreviewClose(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		if (ap.SelectedTab is SourceViewerTab codePreview)
		{
			codePreview.Close?.Invoke(ap);
		}
	}
}

#endif
