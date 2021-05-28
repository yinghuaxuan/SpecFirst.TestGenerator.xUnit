﻿
namespace SpecFirst.Core.Specs.Tests
{
    using System;
    using System.Collections.Generic;
    using Xunit;
    
    public partial class infer_type_from_collection_text
    {
        [Theory]
        [MemberData(nameof(get_test_data))]
        public void infer_type_from_collection_text_tests(string collection, string collection_type, string parsed_collection)
        {
            (string collection_type_output, string parsed_collection_output) = infer_type_from_collection_text_implementation(collection);
            Assert.Equal(collection_type_output, collection_type);
            Assert.Equal(parsed_collection_output, parsed_collection);
        }

        public static IEnumerable<object[]> get_test_data()
        {
            var data = new List<object[]>
            {
                new object[] { "[1, 2, 3, 4]", "integer", "new int[] {1, 2, 3, 4}" }, // integer only
                new object[] { "[3M, 12.5M, 0.0M]", "decimal", "new decimal[] {3M, 12.5M, 0.0M}" }, // decimal only
                new object[] { "[12.5, 3D, 5d, 0.0D]", "double", "new double[] {12.5D, 3D, 5D, 0.0D}" }, // double only
                new object[] { "[input, \"output\", comment, \"-12\"]", "string", "new string[] {\"input\", \"output\", \"comment\", \"-12\"}" }, // string only
                new object[] { "[input, \"output, comment, -12\"]", "string", "new string[] {\"input\", \"\"output\", \"comment\", \"-12\"\"}" }, // string only
                new object[] { "[1, 2, 1M, 2m]", "decimal", "new decimal[] {1, 2, 1M, 2M}" }, // integer and decimal
                new object[] { "[1, 2, 1D, 2d]", "double", "new double[] {1, 2, 1D, 2D}" }, // integer and double
                new object[] { "[1D, 1d, 1M, 1m]", "object", "new object[] {1D, 1D, 1M, 1M}" }, // decimal and double
                new object[] { "[1, 1D, 1d, 1M, 1m, \"1\"]", "object", "new object[] {1, 1D, 1D, 1M, 1M, \"1\"}" }, // number and string
                new object[] { "[1, 1D, 1d, 1M, 1m, \"1\"", "string", "[1, 1D, 1d, 1M, 1m, \"1\"" }, // missing ending bracket
                new object[] { "[[1, 1D, 1d, 1M, 1m, \"1\"]", "string", "[[1, 1D, 1d, 1M, 1m, \"1\"]" }, // extra starting bracket
                new object[] { "[1, 1D, 1d, 1M, 1m, \"1\"]]", "string", "[1, 1D, 1d, 1M, 1m, \"1\"]]" }, // extra ending bracket
                new object[] { "[1,,]", "string", "[1,,]" }, // empty comma
            };

            return data;
        }

        private partial (string, string) infer_type_from_collection_text_implementation(string collection);
    }

}