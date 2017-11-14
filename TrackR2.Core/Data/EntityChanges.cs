using System;
using System.Collections.Generic;
using System.Linq;
using KellermanSoftware.CompareNetObjects;

namespace TrackR2.Core.Data
{
    public class EntityChanges
    { 
        public List<Modification> Modifications { get; }
        public bool IsDifferent => Modifications?.Any() ?? false;

        public string EntityInfo { get; private set; }

        private static readonly ComparisonConfig Config = new ComparisonConfig
        {
            MaxDifferences = Int32.MaxValue,
            CompareReadOnly = false,
            CompareChildren = false,
            CompareFields = false,
            ComparePrivateFields = false,
            ComparePrivateProperties = false,
            CompareStaticFields = false,
            CompareStaticProperties = false,
        };

        private static readonly string[] IgnoredProperties =
        {
            "EnableAutomaticValidation",
        };

        public EntityChanges(object current, object original)
        {
            // 0: Guard
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            if (original == null)
                throw new ArgumentNullException(nameof(original));

            // 1: Initialize properties
            Modifications = new List<Modification>();
            MakeEntityInfo(current);

            // 2: Compare
            var comparison = new CompareLogic(Config);
            var result = comparison.Compare(original, current);
            ParseModifications(result, current);
        }

        private void MakeEntityInfo(object current)
        {
            var id = (int?)current.GetType().GetProperty("Id")?.GetValue(current);
            var type = current.GetType().Name;

            EntityInfo = $"{type} (ID={id?.ToString() ?? "n/a"})";
        }

        private void ParseModifications(ComparisonResult result, object current)
        {
            foreach (var diff in result.Differences)
            {
                var propertyName = diff.PropertyName;
                if (propertyName.StartsWith("."))
                    propertyName = propertyName.Substring(1);

                if (IgnoredProperties.Contains(propertyName))
                    continue;
                
                var modification = new Modification(propertyName, diff.Object1Value, diff.Object2Value);
                Modifications.Add(modification);
            }
        }
    }
}
