﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ov_partial_shape = OpenVinoSharp.Ov.ov_partial_shape;

namespace OpenVinoSharp
{
    /// <summary>
    /// Class representing a shape that may be partially or totally dynamic.
    /// </summary>
    /// <remarks>
    /// <para>Dynamic rank. (Informal notation: `?`)</para>
    /// <para>Static rank, but dynamic dimensions on some or all axes.
    /// (Informal notation examples: `{1,2,?,4}`, `{?,?,?}`)</para>
    /// <para> Static rank, and static dimensions on all axes.
    /// (Informal notation examples: `{1,2,3,4}`, `{6}`, `{}`)</para>
    /// </remarks>
    public class PartialShape
    {
        /// <summary>
        /// [private]Core class pointer.
        /// </summary>
        private IntPtr m_ptr = IntPtr.Zero;
        /// <summary>
        /// [public]Core class pointer.
        /// </summary>
        public IntPtr Ptr { get { return m_ptr; } set { m_ptr = value; } }



        /// <summary>
        /// PartialShape rank.
        /// </summary>
        private Dimension rank;
        
        /// <summary>
        /// PartialShape dimensions.
        /// </summary>
        private Dimension[] dimensions;

        /// <summary>
        /// Constructing partial shape by pointer.
        /// </summary>
        /// <param name="ptr">The partial shape ptr./param>
        public PartialShape(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Shape init error : ptr is null!");
                return;
            }
            this.m_ptr = ptr;
            var temp = Marshal.PtrToStructure(ptr, typeof(ov_partial_shape));
            ov_partial_shape shape = (ov_partial_shape)temp;
            Dimension rank_tmp = new Dimension(shape.rank);

            if (!rank_tmp.is_dynamic()){
                rank = rank_tmp;
                IntPtr[] d_ptr = new IntPtr[rank.get_max()];
                Marshal.Copy(shape.dims, d_ptr, 0, (int)rank.get_min());

                dimensions = new Dimension[rank.get_min()];
                for (int i = 0; i < rank.get_min(); ++i) 
                {
                    var temp1 = Marshal.PtrToStructure(ptr, typeof(Ov.ov_dimension));
                    Dimension dim = new Dimension((Ov.ov_dimension)temp1);
                    dimensions[i] = dim;
                }
            }
            else {
                rank = rank_tmp;
            }
        }
        /// <summary>
        /// Constructing partial shape by dimensions.
        /// </summary>
        /// <param name="dimensions">The partial shape dimensions array.</param>
        public PartialShape(Dimension[] dimensions) 
        { 
            Ov.ov_dimension[] ds = new Ov.ov_dimension[dimensions.Length];
            for (int i = 0; i < dimensions.Length; ++i)
            {
                ds[i] = dimensions[i].get_dimension();
            }
            HandleException.handler(
                NativeMethods.ov_partial_shape_create((long)dimensions.Length, ref ds[0], m_ptr));
            this.dimensions = dimensions;
            rank = new Dimension(dimensions.Length, dimensions.Length);
        }
        /// <summary>
        /// Constructing partial shape by dimensions.
        /// </summary>
        /// <param name="dimensions">The partial shape dimensions list.</param>
        public PartialShape(List<Dimension> dimensions) : this(dimensions.ToArray())
        { 
        }

        /// <summary>
        /// Constructing dynamic partial shape by dimensions.
        /// </summary>
        /// <param name="rank">The partial shape rank.</param>
        /// <param name="dimensions">The partial shape dimensions array.</param>
        public PartialShape(Dimension rank, Dimension[] dimensions)
        {
            Ov.ov_dimension[] ds = new Ov.ov_dimension[dimensions.Length];
            for (int i = 0; i < dimensions.Length; ++i)
            {
                ds[i] = dimensions[i].get_dimension();
            }
            HandleException.handler(
                NativeMethods.ov_partial_shape_create_dynamic(rank.get_dimension(), ref ds[0], m_ptr));
            this.dimensions = dimensions;
            this.rank = rank;
        }

        /// <summary>
        /// Constructing dynamic partial shape by dimensions.
        /// </summary>
        /// <param name="rank">The partial shape rank.</param>
        /// <param name="dimensions">The partial shape dimensions list.</param>
        public PartialShape(Dimension rank, List<Dimension> dimensions) : this(rank, dimensions.ToArray())
        {

        }
        /// <summary>
        /// Constructing static partial shape by dimensions.
        /// </summary>
        /// <param name="rank">The partial shape rank.</param>
        /// <param name="dimensions">The partial shape dimensions array.</param>
        public PartialShape(long rank, long[] dimensions)
        {
            HandleException.handler(
                NativeMethods.ov_partial_shape_create_static(rank, ref dimensions[0], m_ptr));
            this.rank = new Dimension(rank);
            for (int i = 0; i < dimensions.Length; ++i)
            {
                this.dimensions[i] = new Dimension(dimensions[i]);
            }
        }
        /// <summary>
        /// Constructing static partial shape by dimensions.
        /// </summary>
        /// <param name="rank">The partial shape rank.</param>
        /// <param name="dimensions">The partial shape dimensions list.</param>
        public PartialShape(long rank, List<long> dimensions) : this(rank, dimensions.ToArray())
        {}

        /// <summary>
        /// Constructing static partial shape by shape.
        /// </summary>
        /// <param name="shape">The shape</param>
        public PartialShape(Shape shape) 
        {
            HandleException.handler(
                NativeMethods.ov_shape_to_partial_shape(shape.shape, m_ptr));
            this.rank = new Dimension(shape.Count);
            for (int i = 0; i < dimensions.Length; ++i)
            {
                this.dimensions[i] = new Dimension(shape[i]);
            }
        }

        /// <summary>
        /// Default deconstruction.
        /// </summary>
        ~PartialShape()
        {
            dispose();
        }
        /// <summary>
        /// Release unmanaged resources.
        /// </summary>
        public void dispose()
        {
            if (m_ptr == IntPtr.Zero)
            {
                return;
            }
            NativeMethods.ov_partial_shape_free(m_ptr);
            m_ptr = IntPtr.Zero;
        }

        /// <summary>
        /// Get ov_partial_shape
        /// </summary>
        /// <returns>return ov_partial_shape.</returns>
        public ov_partial_shape get_partial_shape() 
        {
            ov_partial_shape partial_shape = new ov_partial_shape();
            partial_shape.rank = rank.get_dimension();
            int l = Marshal.SizeOf(typeof(Ov.ov_dimension));
            IntPtr[] ds_ptr = new IntPtr[rank.get_max()];
            for (int i = 0; i < rank.get_max(); ++i) {
                IntPtr ptr = Marshal.AllocHGlobal(l);
                Marshal.StructureToPtr(dimensions[i], ptr, false);
                ds_ptr[i] = ptr;
            }
            
            IntPtr d_ptr = Marshal.AllocHGlobal((int)(l * rank.get_max()));
            Marshal.Copy(ds_ptr, 0, d_ptr, (int)rank.get_max());
            partial_shape.dims = d_ptr;
            return partial_shape;
        }
        /// <summary>
        /// Get dimensions.
        /// </summary>
        /// <returns>Dimension[</returns>
        public Dimension[] get_dimensions() { 
            return dimensions;
        }

        /// <summary>
        /// Convert partial shape without dynamic data to a static shape.
        /// </summary>
        /// <returns>The shape.</returns>
        public Shape to_shape()
        {
            IntPtr shape_ptr = IntPtr.Zero;
            HandleException.handler(
                NativeMethods.ov_partial_shape_to_shape(get_partial_shape(), shape_ptr));     
            return new Shape(shape_ptr);
        }

        /// <summary>
        /// Check if this shape is static.
        /// </summary>
        /// <remarks>A shape is considered static if it has static rank, and all dimensions of the shape
        /// are static.</remarks>
        /// <returns>`true` if this shape is static, else `false`.</returns>
        public bool is_static() {
            return !is_dynamic();
        }

        /// <summary>
        /// Check if this shape is dynamic.
        /// </summary>
        /// <remarks>A shape is considered static if it has static rank, and all dimensions of the shape
        /// are static.</remarks>
        /// <returns>`false` if this shape is static, else `true`.</returns>
        public bool is_dynamic() {
            return NativeMethods.ov_partial_shape_is_dynamic(get_partial_shape());
        }

        /// <summary>
        /// Get partial shape string.
        /// </summary>
        /// <returns></returns>
        public string to_string() 
        {
            string s = "Shape : {";
            if (rank.is_dynamic())
            {
                s += "?";
            }
            else 
            {
                for (int i = 0; i < rank.get_max(); ++i) 
                {
                    if (dimensions[i].is_dynamic())
                    {
                        s += "?,";
                    }
                    else
                    {
                        s += dimensions[i].get_dimension().max.ToString() + ",";
                    }
                }
            }
            s = s.Substring(0, s.Length - 1);
            s += "}";
            return s;
        }
    }
}
