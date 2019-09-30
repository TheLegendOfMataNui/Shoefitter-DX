﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using System.Collections.Generic;

namespace SAGESharp.IO.Trees
{
    static class TreeWithNestedLists
    {
        public static IDataNode Build() => new BuilderFor.DataNodeSubstitute
        {
            Edges = new List<IEdge>
            {
                new BuilderFor.EdgeSubstitute<Class>
                {
                    ChildNode = new BuilderFor.ListNodeSubstitute<TreeWithHeight1.Class>
                    {
                        ChildNode = TreeWithHeight1.Build(),
                    }.Build(),
                    ChildExtractor = value => value.List1
                }.Build(),
                new BuilderFor.EdgeSubstitute<Class>
                {
                    ChildNode = new BuilderFor.ListNodeSubstitute<TreeWithHeight2.Class>
                    {
                        ChildNode = TreeWithHeight2.Build()
                    }.Build(),
                    ChildExtractor = value => value.List2
                }.Build()
            }
        }.Build();

        public class Class
        {
            public IList<TreeWithHeight1.Class> List1 { get; set; }

            public IList<TreeWithHeight2.Class> List2 { get; set; }
        }
    }
}
