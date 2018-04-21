/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution                      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace ContentAwareFill
{
    internal unsafe sealed class MemoryBlock : IDisposable
    {
        // blocks this size or larger are allocated with AllocateLarge (VirtualAlloc) instead of Allocate (HeapAlloc)
        private const long largeBlockThreshold = 65536;

        private long length;

        // if parentBlock == null, then we allocated the pointer and are responsible for deallocating it
        // if parentBlock != null, then the parentBlock allocated it, not us
        private void* voidStar;

        private bool valid; // if voidStar is null, and this is false, we know that it's null because allocation failed. otherwise we have a real error

        private MemoryBlock parentBlock = null;

        private bool disposed = false;

        public MemoryBlock Parent
        {
            get
            {
                return this.parentBlock;
            }
        }

        public long Length
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return length;
            }
        }

        public IntPtr Pointer
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return new IntPtr(voidStar);
            }
        }

        public void* VoidStar
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return voidStar;
            }
        }

        /// <summary>
        /// Copies bytes from one area of memory to another. Since this function works
        /// with MemoryBlock instances, it does bounds checking.
        /// </summary>
        /// <param name="dst">The MemoryBlock to copy bytes to.</param>
        /// <param name="dstOffset">The offset within dst to copy bytes to.</param>
        /// <param name="src">The MemoryBlock to copy bytes from.</param>
        /// <param name="srcOffset">The offset within src to copy bytes from.</param>
        /// <param name="length">The number of bytes to copy.</param>
        public static void CopyBlock(MemoryBlock dst, long dstOffset, MemoryBlock src, long srcOffset, long length)
        {
            if ((dstOffset + length > dst.length) || (srcOffset + length > src.length))
            {
                throw new ArgumentOutOfRangeException("", "copy ranges were out of bounds");
            }

            if (dstOffset < 0)
            {
                throw new ArgumentOutOfRangeException("dstOffset", dstOffset, "must be >= 0");
            }

            if (srcOffset < 0)
            {
                throw new ArgumentOutOfRangeException("srcOffset", srcOffset, "must be >= 0");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "must be >= 0");
            }

            void* dstPtr = (void*)((byte*)dst.VoidStar + dstOffset);
            void* srcPtr = (void*)((byte*)src.VoidStar + srcOffset);
            Memory.Copy(dstPtr, srcPtr, (ulong)length);
        }

        /// <summary>
        /// Creates a new parent MemoryBlock and copies our contents into it
        /// </summary>
        public MemoryBlock Clone()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MemoryBlock");
            }

            MemoryBlock dupe = new MemoryBlock(this.length);
            CopyBlock(dupe, 0, this, 0, length);
            return dupe;
        }

        /// <summary>
        /// Creates a new MemoryBlock instance and allocates the requested number of bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public MemoryBlock(long bytes)
        {
            if (bytes <= 0)
            {
                throw new ArgumentOutOfRangeException("bytes", bytes, "Bytes must be greater than zero");
            }

            this.length = bytes;
            this.parentBlock = null;
            this.voidStar = Allocate(bytes).ToPointer();
            this.valid = true;
        }

        /// <summary>
        /// Creates a new MemoryBlock instance that refers to part of another MemoryBlock.
        /// The other MemoryBlock is the parent, and this new instance is the child.
        /// </summary>
        public unsafe MemoryBlock(MemoryBlock parentBlock, long offset, long length)
        {
            if (offset + length > parentBlock.length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.parentBlock = parentBlock;
            byte* bytePointer = (byte*)parentBlock.VoidStar;
            bytePointer += offset;
            this.voidStar = (void*)bytePointer;
            this.valid = true;
            this.length = length;
        }

        ~MemoryBlock()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }

                if (this.valid && parentBlock == null)
                {
                    if (this.length >= largeBlockThreshold)
                    {
                        Memory.FreeLarge(new IntPtr(voidStar), (ulong)this.length);
                    }
                    else
                    {
                        Memory.Free(new IntPtr(voidStar));
                    }
                }

                parentBlock = null;
                voidStar = null;
                this.valid = false;
            }
        }


        private static IntPtr Allocate(long bytes)
        {
            return Allocate(bytes, true);
        }

        private static IntPtr Allocate(long bytes, bool allowRetry)
        {
            IntPtr block;

            try
            {
                if (bytes >= largeBlockThreshold)
                {
                    block = Memory.AllocateLarge((ulong)bytes);
                }
                else
                {
                    block = Memory.Allocate((ulong)bytes, true);
                }
            }
            catch (OutOfMemoryException)
            {
                if (allowRetry)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    return Allocate(bytes, false);
                }
                else
                {
                    throw;
                }
            }

            return block;
        }
    }
}
