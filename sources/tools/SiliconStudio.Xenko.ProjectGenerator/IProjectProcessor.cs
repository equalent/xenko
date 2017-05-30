// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System.Xml.Linq;
namespace SiliconStudio.Xenko.ProjectGenerator
{
    public interface IProjectProcessor
    {
        void Process(ProjectProcessorContext context);
    }
}
