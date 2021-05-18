using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAnalyser
{
    internal class HitAnalyser
    {
        List<Vertice> _Vertices;
      

        void Print()
        {
            Console.WriteLine();
            Console.WriteLine("_____________________________________");
            if (_Vertices != null)
                foreach (var x in _Vertices)
                {
                    x.Print();
                }
        else
            Console.WriteLine("List of vertices is empty");
        }


        internal HitAnalyser(Dictionary<String, HashSet<String>> Ce, Dictionary<String, HashSet<String>> Ca)
        {
            _Vertices = new List<Vertice>();
            
            if (Ce.Count != Ca.Count) throw new Exception("Dicitionaries have different sizes");

            foreach(var x in Ce)
            {
                if (!Ca.ContainsKey(x.Key)) throw new Exception("Keys in dictionaries do not match");
                else _Vertices.Add(new Vertice(x.Key, Ce[x.Key], Ca[x.Key]));
            }
            //Print();

        }

        internal List<Vertice> findHubsAndAuthsUsingHITS()
        {
            bool iterate = true;
            int counter = 0;
            while (iterate)
            {
                ScoreNewAuthorities();
                ScoreNewHubs();
                NormaliseHubsAndAuthorities();
                //Use next line for diagnostics
                //Print();
                iterate = isAnyDifference() ? true : false;
                UpdateHubsAndAuthorities();
                if (++counter > 50) break;
   
                
            }


            return _Vertices;
        }

        void UpdateHubsAndAuthorities ()
        {

            foreach (var x in _Vertices)
            {
                x._oldAuthorityValue = x._newAuthorityValue;
                x._oldHubValue = x._newHubValue;
            }
        }

        void ScoreNewAuthorities()
        {
            
            foreach(var x in _Vertices)
            {
                float sum = 0;
                foreach (var y in x._pointedBy)
                {
                    sum+=_Vertices.Where(vertice => vertice._name == y)
                                    .Select(score=>score._oldHubValue).First();
                }
                x._newAuthorityValue = sum;

            }


        }

        void ScoreNewHubs()
        {
            foreach (var x in _Vertices)
            {
                float sum = 0;

                foreach (var y in x._pointsTo)
                {            
                      sum += _Vertices
                         .Where(vertice => vertice._name == y)
                                    .Select(score => score._oldAuthorityValue).First();
                }
                x._newHubValue = sum;
            }
        }

        void NormaliseHubsAndAuthorities()
        {
            float authSum = 0;
            float hubSum = 0;
            authSum = _Vertices.Select(authority => authority._newAuthorityValue).Sum();
            hubSum = _Vertices.Select(hub => hub._newHubValue).Sum();

            foreach (var x in _Vertices)
            {
                if (authSum!=0)
                    x._newAuthorityValue = (float)Math.Round(x._newAuthorityValue / authSum, 3);
                if (hubSum!=0)
                x._newHubValue = (float)Math.Round(x._newHubValue / hubSum, 3);
            }
        }

        bool isAnyDifference()
        {
            foreach (var x in _Vertices)
                if (Math.Round(x._newAuthorityValue, 3) != Math.Round(x._oldAuthorityValue, 3)
                    || Math.Round(x._newHubValue, 3) != Math.Round(x._oldHubValue, 3))
                    return true;
            return false;
        }
    }
}
