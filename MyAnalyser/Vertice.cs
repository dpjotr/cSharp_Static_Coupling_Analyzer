using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAnalyser
{
    internal class Vertice
    {
        internal String _name;

        internal HashSet<String> _pointsTo;
        internal HashSet<String> _pointedBy;

        internal float _oldAuthorityValue;
        internal float _oldHubValue;
        internal float _newAuthorityValue;
        internal float _newHubValue;

        internal void Print()
        {
            Console.WriteLine
                ($"Name: {_name, 115} " +
                $"{_oldAuthorityValue,5}{_oldHubValue,5}" +
                $"{_newAuthorityValue,5}{_newHubValue, 5}");
        }

        
        internal Vertice(String name, HashSet<String> pointsTo, HashSet<String> pointedBy)
        {
            _oldAuthorityValue = 1;
            _oldHubValue = 1;
            _newAuthorityValue = 0;
            _newHubValue = 0;
            _name = name;
            _pointsTo = pointsTo;
            _pointedBy = pointedBy;
        }
    }
}
