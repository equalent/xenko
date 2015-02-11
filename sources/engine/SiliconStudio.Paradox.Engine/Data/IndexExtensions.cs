// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Core;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics.Data;

namespace SiliconStudio.Paradox.Extensions
{
    public static class IndexExtensions
    {
        /// <summary>
        /// Generates an index buffer for this mesh data.
        /// </summary>
        /// <param name="meshData">The mesh data.</param>
        /// <param name="declaration">The final vertex declaration</param>
        public unsafe static void GenerateIndexBuffer(this MeshDraw meshData, VertexDeclaration declaration)
        {
            // For now, require a MeshData with only one vertex buffer and no index buffer
            if (meshData.VertexBuffers.Length != 1 || meshData.IndexBuffer != null)
                throw new NotImplementedException();

            var oldVertexBuffer = meshData.VertexBuffers[0];
            var oldVertexStride = oldVertexBuffer.Declaration.VertexStride;
            var newVertexStride = declaration.VertexStride;
            var indexMapping = GenerateIndexMapping(oldVertexBuffer);
            var vertices = indexMapping.Vertices;

            // Generate vertex buffer
            var vertexBufferData = new byte[declaration.VertexStride * indexMapping.Vertices.Length];
            fixed (byte* oldVertexBufferDataStart = &oldVertexBuffer.Buffer.GetSerializationData().Content[oldVertexBuffer.Offset])
            fixed (byte* vertexBufferDataStart = &vertexBufferData[0])
            {
                var vertexBufferDataCurrent = vertexBufferDataStart;
                for (int i = 0; i < vertices.Length; ++i)
                {
                    Utilities.CopyMemory((IntPtr)vertexBufferDataCurrent, new IntPtr(&oldVertexBufferDataStart[oldVertexStride * vertices[i]]), newVertexStride);
                    vertexBufferDataCurrent += newVertexStride;
                }
                meshData.VertexBuffers[0] = new VertexBufferBinding(new BufferData(BufferFlags.VertexBuffer, vertexBufferData).ToSerializableVersion(), declaration, indexMapping.Vertices.Length);
            }

            // Generate index buffer
            var indexBufferData = new byte[indexMapping.Indices.Length * Utilities.SizeOf<int>()];
            fixed (int* indexDataStart = &indexMapping.Indices[0])
            fixed (byte* indexBufferDataStart = &indexBufferData[0])
            {
                Utilities.CopyMemory((IntPtr)indexBufferDataStart, (IntPtr)indexDataStart, indexBufferData.Length);
                meshData.IndexBuffer = new IndexBufferBinding(new BufferData(BufferFlags.IndexBuffer, indexBufferData).ToSerializableVersion(), true, indexMapping.Indices.Length);
            }
        }

        /// <summary>
        /// Compacts the index buffer from 32 bits to 16 bits per index, if possible.
        /// </summary>
        /// <param name="meshData">The mesh data.</param>
        /// <returns>Returns true if index buffer was actually compacted.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static unsafe bool CompactIndexBuffer(this MeshDraw meshData)
        {
            // Already processed?
            if (meshData.IndexBuffer == null || !meshData.IndexBuffer.Is32Bit)
                return false;

            // For now, require a MeshData with only one vertex buffer and no index buffer
            if (meshData.VertexBuffers.Length != 1)
                throw new NotImplementedException();

            var vertexBufferBinding = meshData.VertexBuffers[0];
            var vertexCount = vertexBufferBinding.Count;

            // Can't compact?
            // Note that 65536 could be valid, but 0xFFFF is kept for primitive restart in strips.
            if (vertexCount >= 65536 || !meshData.IndexBuffer.Is32Bit)
                return false;

            // Create new index buffer
            var indexCount = meshData.IndexBuffer.Count;
            var indexBufferData = new byte[indexCount * Utilities.SizeOf<ushort>()];
            fixed (byte* oldIndexBufferDataStart = &meshData.IndexBuffer.Buffer.GetSerializationData().Content[0])
            fixed (byte* indexBufferDataStart = &indexBufferData[0])
            {
                var oldIndexBufferDataPtr = (int*)oldIndexBufferDataStart;
                var indexBufferDataPtr = (ushort*)indexBufferDataStart;

                for (int i = 0; i < indexCount; ++i)
                {
                    // This also works to convert 0xFFFFFFFF into 0xFFFF (primitive restart in strips).
                    *indexBufferDataPtr++ = (ushort)*oldIndexBufferDataPtr++;
                }

                meshData.IndexBuffer = new IndexBufferBinding(new BufferData(BufferFlags.IndexBuffer, indexBufferData).ToSerializableVersion(), false, indexCount);
            }

            return true;
        }

        /// <summary>
        /// Generates the index buffer with dominant edge and vertex informations.
        /// Each triangle gets its indices expanded to 12 control points, with 0 to 2 being original triangle,
        /// 3 to 8 being dominant edges and 9 to 11 being dominant vertices.
        /// </summary>
        /// <param name="meshData">The mesh data.</param>
        public static unsafe void GenerateIndexBufferAEN(this MeshDraw meshData)
        {
            // For now, require a MeshData with only one vertex buffer and one index buffer
            if (meshData.VertexBuffers.Length != 1 || meshData.IndexBuffer == null)
                throw new NotImplementedException();

            // More info at http://developer.download.nvidia.com/whitepapers/2010/PN-AEN-Triangles-Whitepaper.pdf
            // This implementation might need some performance improvements
            var indexBuffer = meshData.IndexBuffer;

            var triangleCount = indexBuffer.Count / 3;
            var newIndices = new int[triangleCount * 12];

            var positionMapping = GenerateIndexMapping(meshData.VertexBuffers[0], "POSITION");
            var dominantEdges = new Dictionary<EdgeKeyAEN, EdgeAEN>();
            var dominantVertices = new Dictionary<int, int>();

            fixed (byte* indexBufferStart = &indexBuffer.Buffer.GetSerializationData().Content[indexBuffer.Offset])
            {
                var oldIndices = (int*)indexBufferStart;
                var triangleIndices = stackalloc int[3];
                var positionIndices = stackalloc int[3];

                // Step 2: prepare initial data
                for (int i = 0; i < triangleCount; ++i)
                {
                    triangleIndices[0] = oldIndices[i * 3 + 0];
                    triangleIndices[1] = oldIndices[i * 3 + 1];
                    triangleIndices[2] = oldIndices[i * 3 + 2];

                    positionIndices[0] = positionMapping.Indices[triangleIndices[0]];
                    positionIndices[1] = positionMapping.Indices[triangleIndices[1]];
                    positionIndices[2] = positionMapping.Indices[triangleIndices[2]];

                    newIndices[i * 12 + 0] = triangleIndices[0];
                    newIndices[i * 12 + 1] = triangleIndices[1];
                    newIndices[i * 12 + 2] = triangleIndices[2];
                    newIndices[i * 12 + 3] = triangleIndices[0];
                    newIndices[i * 12 + 4] = triangleIndices[1];
                    newIndices[i * 12 + 5] = triangleIndices[1];
                    newIndices[i * 12 + 6] = triangleIndices[2];
                    newIndices[i * 12 + 7] = triangleIndices[2];
                    newIndices[i * 12 + 8] = triangleIndices[0];
                    newIndices[i * 12 + 9] = triangleIndices[0];
                    newIndices[i * 12 + 10] = triangleIndices[1];
                    newIndices[i * 12 + 11] = triangleIndices[2];

                    // Step 2b/2c: Build dominant vertex/edge list
                    for (int j = 0; j < 3; ++j)
                    {
                        dominantVertices[positionIndices[j]] = triangleIndices[j];

                        var edge = new EdgeAEN(
                            triangleIndices[((j + 0) % 3)],
                            triangleIndices[((j + 1) % 3)],
                            positionIndices[((j + 0) % 3)],
                            positionIndices[((j + 1) % 3)]);

                        dominantEdges[new EdgeKeyAEN(edge)] = edge;

                        edge = edge.Reverse();
                        dominantEdges[new EdgeKeyAEN(edge)] = edge;
                    }
                }

                // Step3: Find dominant vertex/edge
                for (int i = 0; i < triangleCount; ++i)
                {
                    triangleIndices[0] = oldIndices[i * 3 + 0];
                    triangleIndices[1] = oldIndices[i * 3 + 1];
                    triangleIndices[2] = oldIndices[i * 3 + 2];

                    positionIndices[0] = positionMapping.Indices[triangleIndices[0]];
                    positionIndices[1] = positionMapping.Indices[triangleIndices[1]];
                    positionIndices[2] = positionMapping.Indices[triangleIndices[2]];

                    for (int j = 0; j < 3; ++j)
                    {
                        // Dominant edge
                        int vertexKey;
                        if (dominantVertices.TryGetValue(positionIndices[j], out vertexKey))
                        {
                            newIndices[i * 12 + 9 + j] = vertexKey;
                        }

                        // Dominant vertex
                        EdgeAEN edge;
                        var edgeKey = new EdgeKeyAEN(positionIndices[((j + 0) % 3)], positionIndices[((j + 1) % 3)]);
                        if (dominantEdges.TryGetValue(edgeKey, out edge))
                        {
                            newIndices[i * 12 + 3 + j * 2 + 0] = edge.Index0;
                            newIndices[i * 12 + 3 + j * 2 + 1] = edge.Index1;
                        }
                    }
                }
            }

            // Generate index buffer
            var indexBufferData = new byte[triangleCount * 12 * Utilities.SizeOf<int>()];
            fixed (int* indexDataStart = &newIndices[0])
            fixed (byte* indexBufferDataStart = &indexBufferData[0])
            {
                Utilities.CopyMemory((IntPtr)indexBufferDataStart, (IntPtr)indexDataStart, indexBufferData.Length);
                meshData.IndexBuffer = new IndexBufferBinding(new BufferData(BufferFlags.IndexBuffer, indexBufferData).ToSerializableVersion(), true, triangleCount * 12);
            }

            meshData.DrawCount = triangleCount * 12;
            meshData.PrimitiveType = PrimitiveType.PatchList.ControlPointCount(12);
        }

        private struct EdgeKeyAEN
        {
            public readonly int PositionIndex0;
            public readonly int PositionIndex1;

            public EdgeKeyAEN(int positionIndex0, int positionIndex1)
            {
                PositionIndex0 = positionIndex0;
                PositionIndex1 = positionIndex1;
            }

            public EdgeKeyAEN(EdgeAEN edge)
                : this(edge.PositionIndex0, edge.PositionIndex1)
            {
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (PositionIndex0 * 397) ^ PositionIndex1;
                }
            }
        }

        private struct EdgeAEN
        {
            public readonly int Index0;
            public readonly int Index1;
            public readonly int PositionIndex0;
            public readonly int PositionIndex1;

            public EdgeAEN(int index0, int index1, int positionIndex0, int positionIndex1)
            {
                Index0 = index0;
                Index1 = index1;
                PositionIndex0 = positionIndex0;
                PositionIndex1 = positionIndex1;
            }

            public EdgeAEN Reverse()
            {
                return new EdgeAEN(Index1, Index0, PositionIndex1, PositionIndex0);
            }
        }

        /// <summary>
        /// Generates an index mapping with the specified vertex elements.
        /// If no vertex elements are specified, use the whole vertex.
        /// </summary>
        /// <param name="vertexBufferBinding">The vertex buffer binding.</param>
        /// <param name="usages">The vertex element usages to consider.</param>
        /// <returns></returns>
        public static unsafe IndexMappingResult GenerateIndexMapping(this VertexBufferBinding vertexBufferBinding, params string[] usages)
        {
            var bufferData = vertexBufferBinding.Buffer.GetSerializationData().Content;
            var vertexStride = vertexBufferBinding.Declaration.VertexStride;
            var vertexCount = vertexBufferBinding.Count;
            var activeBytes = stackalloc byte[vertexStride];

            var vertexMapping = new List<int>();
            var indexMapping = new int[vertexCount];

            var mapping = new Dictionary<VertexKey, int>(new VertexKeyEqualityComparer(activeBytes, vertexStride));

            // Create a "mask" of part of the vertices that will be used
            // TODO: Use bit packing?
            for (int i = 0; i < vertexBufferBinding.Declaration.VertexStride; ++i)
            {
                activeBytes[i] = (byte)(usages.Length == 0 ? 1 : 0);
            }

            foreach (var vertexElement in vertexBufferBinding.Declaration.EnumerateWithOffsets())
            {
                if (usages.Contains(vertexElement.VertexElement.SemanticAsText))
                {
                    for (int i = 0; i < vertexElement.Size; ++i)
                    {
                        activeBytes[vertexElement.Offset + i] = 1;
                    }
                }
            }

            // Generate index buffer
            fixed (byte* bufferPointerStart = &bufferData[vertexBufferBinding.Offset])
            {
                var bufferPointer = bufferPointerStart;

                for (int i = 0; i < vertexCount; ++i)
                {
                    // Create VertexKey (will generate hash)
                    var vertexKey = new VertexKey(bufferPointer, activeBytes, vertexStride);

                    // Get or create new index
                    int currentIndex;
                    if (!mapping.TryGetValue(vertexKey, out currentIndex))
                    {
                        currentIndex = vertexMapping.Count;
                        mapping.Add(vertexKey, currentIndex);
                        vertexMapping.Add(i);
                    }

                    // Assign index in result buffer
                    indexMapping[i] = currentIndex;

                    bufferPointer += vertexStride;
                }
            }

            return new IndexMappingResult { Vertices = vertexMapping.ToArray(), Indices = indexMapping };
        }

        public struct IndexMappingResult
        {
            public int[] Vertices;
            public int[] Indices;
        }

        internal unsafe struct VertexKey
        {
            public byte* Vertex;
            public int Hash;

            public VertexKey(byte* vertex, byte* activeBytes, int vertexStride)
            {
                Vertex = vertex;
                unchecked
                {
                    Hash = 0;
                    for (int i = 0; i < vertexStride; ++i)
                    {
                        if (*activeBytes++ != 0)
                            Hash = (Hash * 31) ^ *vertex;
                        vertex++;
                    }
                }
            }
        }

        internal unsafe class VertexKeyEqualityComparer : EqualityComparer<VertexKey>
        {
            private byte* activeBytes;
            private int vertexStride;

            public VertexKeyEqualityComparer(byte* activeBytes, int vertexStride)
            {
                this.activeBytes = activeBytes;
                this.vertexStride = vertexStride;
            }

            public override bool Equals(VertexKey x, VertexKey y)
            {
                var vertex1 = x.Vertex;
                var vertex2 = y.Vertex;

                var currentActiveBytes = activeBytes;

                for (int i = 0; i < vertexStride; ++i)
                {
                    if (*currentActiveBytes++ != 0)
                    {
                        if (*vertex1 != *vertex2)
                            return false;
                    }

                    vertex1++;
                    vertex2++;
                }

                return true;
            }

            public override int GetHashCode(VertexKey obj)
            {
                return obj.Hash;
            }
        }
    }
}