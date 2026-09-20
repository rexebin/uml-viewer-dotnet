using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UmlViewer.Core.Extraction;

namespace UmlViewer.Core.Tests.Extraction;

/// <summary>
/// Covers <see cref="StructuralExtractor"/>'s handling of a source-declared type kind it
/// doesn't classify (e.g. a delegate). Deliberately uses a standalone compilation rather
/// than the shared <see cref="SampleProjectCompilationFixture"/>: adding an unsupported
/// type to that shared fixture would make every extraction test throw, since
/// <see cref="StructuralExtractor.Extract"/> eagerly maps every source-declared type.
/// </summary>
public class StructuralExtractorUnsupportedKindTests
{
    [Fact]
    public void Extract_SourceDeclaredDelegate_ThrowsNotSupportedException()
    {
        var tree = CSharpSyntaxTree.ParseText("public delegate void Handler();");
        var compilation = CSharpCompilation.Create(
            "UnsupportedKindFixture",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var ex = Assert.Throws<NotSupportedException>(() => StructuralExtractor.Extract(compilation));
        Assert.Contains("Delegate", ex.Message);
    }
}
