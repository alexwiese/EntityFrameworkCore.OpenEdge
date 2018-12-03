using Microsoft.EntityFrameworkCore.Design;

namespace EntityFrameworkCore.OpenEdge.Design.Internal
{
    public class OpenEdgeAnnotationCodeGenerator : AnnotationCodeGenerator
    {
        public OpenEdgeAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies) : base(dependencies)
        {
        }
    }
}