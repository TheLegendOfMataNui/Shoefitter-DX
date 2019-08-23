﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using SAGESharp.IO;
using System.Collections.Generic;

namespace SAGESharp.SLB.Level
{
    public sealed class SPLinePathsTable
    {
        [SerializableProperty(1)]
        public IList<SplinePath> Entries { get; set; }
    }
}