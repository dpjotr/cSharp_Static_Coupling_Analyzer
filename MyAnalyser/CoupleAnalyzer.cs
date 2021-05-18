using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAnalyser
{
    class CoupleAnalyzer
    {

        Compilation _compilation;
        Solution _solution;
        Project _project;
        List<Document> _projectFiles;
        private Dictionary<String, HashSet<String>> _Ce;
   

        internal CoupleAnalyzer(Compilation c, Solution s, Project p)
        {
            
            _compilation = c;
            _solution = s;
            _project = p;
            _projectFiles = _project.Documents.ToList();


        }

         internal void doCoupleAnalysis()
         {


            Console.WriteLine("Coupling analysis results:");
            Console.WriteLine("________________________________________________________________________________");
            var Ce = _Ce;
            var Ca =  ClassCoupling_CA();
            var CBO =  ClassCoupling_CBO();
            var I =  ClassCoupling_I();
            HitAnalyser hits = new HitAnalyser(Ce, Ca);
            var HITS = hits.findHubsAndAuthsUsingHITS();
            int couplableClasses = HITS.Count;

            var signCe = "Ce";
            var signCa = "Ca";
            var signCBO = "CBO";
            var signCOF = "COF";//Coupling factor: class_cbo/project_cbo
            var signI = "I";
            var signWCBO_Ce = "W_Ce";
            var signWCBO_Ca = "W_Ca";
            var signW_I = "W_I";
            var signClass = "Full Class Name";


            Console.WriteLine($"{signClass,80} " +
                                $"{signCBO,10}" +
                                $"{signCOF,10}" +
                                $"{signCe,10}" +
                                $"{signCa,10}" +
                                $"{signI,10}" +
                                $"{signWCBO_Ce,10}" +
                                $"{signWCBO_Ca,10}" +
                                $"{signW_I, 10}"
                                );
            Console.WriteLine();

            int totalProjectCoupling = HITS.Select(x=> valueGetter(Ce)[x._name]).Sum();
            
            HITS = HITS.OrderByDescending(b => valueGetter(CBO)[b._name]).ToList();

            foreach (var x in HITS)

            {
                var classCOF = totalProjectCoupling == 0 ? 0.0
                    : Math.Round(valueGetter(CBO)[x._name] / (float)totalProjectCoupling, 3);

                if (valueGetter(CBO)[x._name] == 0) break;
                var wI = x._newAuthorityValue + x._newHubValue == 0 ? 0.0
                    : Math.Round(x._newHubValue/(x._newAuthorityValue + x._newHubValue), 3);

                var cbo = valueGetter(CBO)[x._name];

                if (cbo > 10) Console.ForegroundColor = ConsoleColor.DarkRed;
                //char delimeter = '.';
                Console.WriteLine($"{x._name,80}" +
                    $"{cbo,10}" +
                    $"{classCOF,10}" +
                    $"{valueGetter(Ce)[x._name],10}" +
                    $"{valueGetter(Ca)[x._name],10}" +
                    $"{I[x._name],10}" +
                    $"{x._newHubValue,10}" +
                    $"{x._newAuthorityValue,10}" +
                    $"{wI, 10}"
                    );

                if (cbo > 10) Console.ForegroundColor = ConsoleColor.Black;

            }

            var applicationCOF 
                = couplableClasses * (couplableClasses - 1) == 0 ? 0:
                    ((double) totalProjectCoupling)*2 / (double)(couplableClasses * (couplableClasses - 1));
            applicationCOF = Math.Round(applicationCOF, 4);

            Console.WriteLine("________________________________________________________________________________");
            Console.WriteLine($"Total amount of classes in the project which could be coupled: {couplableClasses,10}");
            Console.WriteLine($"Total coupling of the project:                                 {totalProjectCoupling,10}");
            Console.WriteLine($"COF            of the project:                                 {applicationCOF,10}");

        }


        //Ce (Efferent Coupling)- The number of classes that the class depend upon
        //Method returns dictionary of classes of the project with sets of coupling classes
        //Ce does not include nested classes
        internal async void ClassCoupling_CE()
        {
            Dictionary<String, HashSet<String>> classCouplingCE 
                = new Dictionary<string, HashSet<string>>();

            var allAbstractsInCompilationSymbols = _compilation
                .GetSymbolsWithName(x => true)
                .Where(y => y.IsAbstract == true)
                .ToHashSet();

            var allAbstractsInCompilation = allAbstractsInCompilationSymbols
                .Select(z => z.ToString()).ToHashSet();

     
            var allDescendantsOfAbstracts = new HashSet<String>();

            foreach (var x in allAbstractsInCompilationSymbols)
            {
                var y = await Microsoft.CodeAnalysis.FindSymbols
                    .SymbolFinder.FindImplementationsAsync(x,
                        _solution, _solution.Projects.ToImmutableHashSet());
                foreach (var z in y)
                {
                    allDescendantsOfAbstracts.Add(z.ToString());
                }
            }

    
            var allDeclaredClasses = new HashSet<String>();
            var allNested = new HashSet<String>();

            foreach (var proj in _projectFiles)
            {
                SyntaxTree syntaxTree;
                if (proj.TryGetSyntaxTree(out syntaxTree))
                {
                    var model = _compilation.GetSemanticModel(syntaxTree);
                    var root = syntaxTree.GetRoot();

                    var classNodes = root.DescendantNodes()
                            .OfType<ClassDeclarationSyntax>();
                    

                    foreach (var cl in classNodes)
                    {                        
                        var declaredClassName = model.GetDeclaredSymbol(cl).OriginalDefinition.ToString();
                        
                        if (!allDeclaredClasses.Contains(declaredClassName))
                            allDeclaredClasses.Add(declaredClassName);

                        var nested = cl.DescendantNodes().OfType<ClassDeclarationSyntax>()
                            .Select(fieldType => model.GetDeclaredSymbol(fieldType)
                                .OriginalDefinition.ToString()).ToHashSet<String>();
                        allNested = allNested.Union(nested).ToHashSet();

                    }
                }

                allDeclaredClasses = allDeclaredClasses.Except(allAbstractsInCompilation)
                    .ToHashSet();
                allDeclaredClasses = allDeclaredClasses.Except(allDescendantsOfAbstracts)
                    .ToHashSet();
                allDeclaredClasses = allDeclaredClasses.Except(allNested)
                    .ToHashSet();

            }

            foreach (var proj in _projectFiles)
            {
                SyntaxTree syntaxTree;
                if(proj.TryGetSyntaxTree(out syntaxTree))
                {
                    var model = _compilation.GetSemanticModel(syntaxTree);
                    var root = syntaxTree.GetRoot();
                    var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();


                    foreach (var cl in classNodes)
                    {
                        String currentClassName = model.GetDeclaredSymbol(cl)
                                                                .OriginalDefinition.ToString();

                        if (allAbstractsInCompilation.Contains(currentClassName)) break;
                        if (allDescendantsOfAbstracts.Contains(currentClassName)) break;
                        if (allNested.Contains(currentClassName)) break;
                        

                        //Declared classes
                        var fields = cl.DescendantNodes().OfType<FieldDeclarationSyntax>()
                            .Select(fieldType => model.GetTypeInfo(fieldType.Declaration.Type)
                                .Type.ToString()).ToHashSet();

                        //Use next 3 lines for logging and diagnostic, otherwise comment
                        //Console.WriteLine("Class named " + currentClassName
                        //                        + " declared as own field: ");
                        //foreach (var field in fields) Console.WriteLine(field);

                        HashSet<string> identifiers = new HashSet<string>();
                        var descendantIdentifiers = cl.DescendantNodes().OfType<IdentifierNameSyntax>();

                        foreach(var x in descendantIdentifiers)
                        {
                            if (model.GetSymbolInfo(x).Symbol == null) break;
                            else if (model.GetSymbolInfo(x).Symbol.Kind.ToString() == "NamedType")
                                identifiers.Add(model.GetSymbolInfo(x).Symbol.ToString());
                            else if (model.GetSymbolInfo(x).Symbol.ContainingSymbol!=null)
                                identifiers.Add(model.GetSymbolInfo(x).Symbol.ContainingSymbol.ToString());
                        }

                        //Use next 3 lines for logging and diagnostic, otherwise comment
                        //Console.WriteLine("Class named " + currentClassName
                        //                + " has references on data or invocation of method of: ");
                        //foreach (var identifier in identifiers) Console.WriteLine(identifier);

                        var summary = fields
                                 .Union(identifiers).ToHashSet<String>()
                                 .Intersect(allDeclaredClasses).ToHashSet<String>();
                        ;

                        // It is necessary to remove estimated class and its nested classes from summary 
                        var nestedClasses = cl.DescendantNodes().OfType<ClassDeclarationSyntax>()
                            .Select(fieldType => model.GetDeclaredSymbol(fieldType)
                                .OriginalDefinition.ToString()).ToHashSet<String>();

                        foreach (var nested in nestedClasses) summary.Remove(nested);
                        summary.Remove(currentClassName);

                        //Use next 3 lines for logging and diagnostic, otherwise comment
                        //Console.WriteLine("Class named " + currentClassName
                        //                + " has total dependencies on classes: ");
                        //foreach (var x in summary) Console.WriteLine(x);

                        if (!classCouplingCE.ContainsKey(currentClassName))
                            classCouplingCE.Add(key: currentClassName, value: summary);
                        else
                            classCouplingCE[currentClassName].Union(summary);

                    }
                }
            }
            _Ce = classCouplingCE == null ? new Dictionary<string, HashSet<string>>():classCouplingCE;
        }

        //Gets numerical values (strings count) from string sets in dictionary, usable for
        //methods: ClassCoupling_CA, ClassCoupling_CE,ClassCoupling_CBO
        internal Dictionary<String, int> valueGetter(Dictionary<String, HashSet<String>> source)
        {
            Dictionary<String, int> values = new Dictionary<string, int>();
            foreach (var x in source)
                values.Add(key: x.Key, value: x.Value.Count);

            return values;
        }

        internal Dictionary<String, double> ClassCoupling_I()
        {
            Dictionary<String, double> values = new Dictionary<string, double>();
            var Ce = valueGetter(_Ce);
            var Ca = valueGetter(ClassCoupling_CA());

            foreach (var x in Ca)
            {
                var xCa = Convert.ToDouble(Ca[x.Key]);
                var xCe = Convert.ToDouble(Ce[x.Key]);
                //if xCe + xCa == 0, it means that class does not have any coupling at all
                var xI = (xCe + xCa) == 0 ? 0 : Math.Round(xCe / (xCe+xCa),3);
                values.Add(key: x.Key, value: xI);
            }

            return values;
        }

        //Ca (Afferent Coupling)- The number of classes that depend upon the target class 
        //Method returns dictionary of classes of the project with sets of coupled classes
        internal Dictionary<String, HashSet<String>> ClassCoupling_CA() 
        {
            var classCouplingCE = _Ce;
            Dictionary<String, HashSet<String>> classCouplingCA 
                = new Dictionary<string, HashSet<string>>();
            foreach(var estimatedClass in classCouplingCE)
            {
                classCouplingCA.Add(key: estimatedClass.Key, value: new HashSet<string>());
                foreach(var  outerClass in classCouplingCE)
                {
                    if (estimatedClass.Key != outerClass.Key)
                    {
                        if (classCouplingCE[outerClass.Key].Contains(estimatedClass.Key))
                        {
                            classCouplingCA[estimatedClass.Key].Add(outerClass.Key);
                        }
                    }
                }
            }
            //Use next 6 lines for logging and diagnostic, otherwise comment
            //foreach (var dictPair in classCouplingCA)
            // {
            //    Console.WriteLine(dictPair.Key + " coupled classes:");
            //    foreach (var valueClass in dictPair.Value)
            //        Console.WriteLine(valueClass);
            //}

            return classCouplingCA;
        }

        internal Dictionary<String, HashSet<String>> ClassCoupling_CBO()
        {
            Dictionary<String, HashSet<String>> classCouplingCBO
                = new Dictionary<string, HashSet<string>>();
            var CeDict = _Ce;
            var CaDict =   ClassCoupling_CA();

            foreach (var x in CeDict)
            {
                classCouplingCBO
                    .Add(key: x.Key, value: CeDict[x.Key].Union(CaDict[x.Key]).ToHashSet<String>());
            }
            //Use next 6 lines for logging and diagnostic, otherwise comment
            //foreach (var dictPair in classCouplingCBO)
            //{
            //    Console.WriteLine(dictPair.Key + " coupled classes:");
            //    foreach (var valueClass in dictPair.Value)
            //        Console.WriteLine(valueClass);
            //    Console.WriteLine();
            //}

            return classCouplingCBO;
        }
    }
}
