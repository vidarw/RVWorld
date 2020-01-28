// Inftree.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.  
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License. 
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs): 
// Time-stamp: <2009-October-28 12:43:54>
//
// ------------------------------------------------------------------
//
// This module defines classes used in  decompression. This code is derived
// from the jzlib implementation of zlib. In keeping with the license for jzlib, 
// the copyright to that code is below.
//
// ------------------------------------------------------------------
// 
// Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright 
// notice, this list of conditions and the following disclaimer in 
// the documentation and/or other materials provided with the distribution.
// 
// 3. The names of the authors may not be used to endorse or promote products
// derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
// INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
// -----------------------------------------------------------------------
//
// This program is based on zlib-1.1.3; credit to authors
// Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
// and contributors of zlib.
//
// -----------------------------------------------------------------------


using System;

namespace ZLib
{

    sealed class InfTree
    {

        private const int MANY = 1440;

        private const int Z_OK = 0;
        private const int Z_STREAM_END = 1;
        private const int Z_NEED_DICT = 2;
        private const int Z_ERRNO = -1;
        private const int Z_STREAM_ERROR = -2;
        private const int Z_DATA_ERROR = -3;
        private const int Z_MEM_ERROR = -4;
        private const int Z_BUF_ERROR = -5;
        private const int Z_VERSION_ERROR = -6;

        private const int fixed_bl = 9;
        private const int fixed_bd = 5;

        //UPGRADE_NOTE: Final was removed from the declaration of 'fixed_tl'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] fixed_tl = new int[]{96, 7, 256, 0, 8, 80, 0, 8, 16, 84, 8, 115, 82, 7, 31, 0, 8, 112, 0, 8, 48, 0, 9, 192, 80, 7, 10, 0, 8, 96, 0, 8, 32, 0, 9, 160, 0, 8, 0, 0, 8, 128, 0, 8, 64, 0, 9, 224, 80, 7, 6, 0, 8, 88, 0, 8, 24, 0, 9, 144, 83, 7, 59, 0, 8, 120, 0, 8, 56, 0, 9, 208, 81, 7, 17, 0, 8, 104, 0, 8, 40, 0, 9, 176, 0, 8, 8, 0, 8, 136, 0, 8, 72, 0, 9, 240, 80, 7, 4, 0, 8, 84, 0, 8, 20, 85, 8, 227, 83, 7, 43, 0, 8, 116, 0, 8, 52, 0, 9, 200, 81, 7, 13, 0, 8, 100, 0, 8, 36, 0, 9, 168, 0, 8, 4, 0, 8, 132, 0, 8, 68, 0, 9, 232, 80, 7, 8, 0, 8, 92, 0, 8, 28, 0, 9, 152, 84, 7, 83, 0, 8, 124, 0, 8, 60, 0, 9, 216, 82, 7, 23, 0, 8, 108, 0, 8, 44, 0, 9, 184, 0, 8, 12, 0, 8, 140, 0, 8, 76, 0, 9, 248, 80, 7, 3, 0, 8, 82, 0, 8, 18, 85, 8, 163, 83, 7, 35, 0, 8, 114, 0, 8, 50, 0, 9, 196, 81, 7, 11, 0, 8, 98, 0, 8, 34, 0, 9, 164, 0, 8, 2, 0, 8, 130, 0, 8, 66, 0, 9, 228, 80, 7, 7, 0, 8, 90, 0, 8, 26, 0, 9, 148, 84, 7, 67, 0, 8, 122, 0, 8, 58, 0, 9, 212, 82, 7, 19, 0, 8, 106, 0, 8, 42, 0, 9, 180, 0, 8, 10, 0, 8, 138, 0, 8, 74, 0, 9, 244, 80, 7, 5, 0, 8, 86, 0, 8, 22, 192, 8, 0, 83, 7, 51, 0, 8, 118, 0, 8, 54, 0, 9, 204, 81, 7, 15, 0, 8, 102, 0, 8, 38, 0, 9, 172, 0, 8, 6, 0, 8, 134, 0, 8, 70, 0, 9, 236, 80, 7, 9, 0, 8, 94, 0, 8, 30, 0, 9, 156, 84, 7, 99, 0, 8, 126, 0, 8, 62, 0, 9, 220, 82, 7, 27, 0, 8, 110, 0, 8, 46, 0, 9, 188, 0, 8, 14, 0, 8, 142, 0, 8, 78, 0, 9, 252, 96, 7, 256, 0, 8, 81, 0, 8, 17, 85, 8, 131, 82, 7, 31, 0, 8, 113, 0, 8, 49, 0, 9, 194, 80, 7, 10, 0, 8, 97, 0, 8, 33, 0, 9, 162, 0, 8, 1, 0, 8, 129, 0, 8, 65, 0, 9, 226, 80, 7, 6, 0, 8, 89, 0, 8, 25, 0, 9, 146, 83, 7, 59, 0, 8, 121, 0, 8, 57, 0, 9, 210, 81, 7, 17, 0, 8, 105, 0, 8, 41, 0, 9, 178, 0, 8, 9, 0, 8, 137, 0, 8, 73, 0, 9, 242, 80, 7, 4, 0, 8, 85, 0, 8, 21, 80, 8, 258, 83, 7, 43, 0, 8, 117, 0, 8, 53, 0, 9, 202, 81, 7, 13, 0, 8, 101, 0, 8, 37, 0, 9, 170, 0, 8, 5, 0, 8, 133, 0, 8, 69, 0, 9, 234, 80, 7, 8, 0, 8, 93, 0, 8, 29, 0, 9, 154, 84, 7, 83, 0, 8, 125, 0, 8, 61, 0, 9, 218, 82, 7, 23, 0, 8, 109, 0, 8, 45, 0, 9, 186,
                        0, 8, 13, 0, 8, 141, 0, 8, 77, 0, 9, 250, 80, 7, 3, 0, 8, 83, 0, 8, 19, 85, 8, 195, 83, 7, 35, 0, 8, 115, 0, 8, 51, 0, 9, 198, 81, 7, 11, 0, 8, 99, 0, 8, 35, 0, 9, 166, 0, 8, 3, 0, 8, 131, 0, 8, 67, 0, 9, 230, 80, 7, 7, 0, 8, 91, 0, 8, 27, 0, 9, 150, 84, 7, 67, 0, 8, 123, 0, 8, 59, 0, 9, 214, 82, 7, 19, 0, 8, 107, 0, 8, 43, 0, 9, 182, 0, 8, 11, 0, 8, 139, 0, 8, 75, 0, 9, 246, 80, 7, 5, 0, 8, 87, 0, 8, 23, 192, 8, 0, 83, 7, 51, 0, 8, 119, 0, 8, 55, 0, 9, 206, 81, 7, 15, 0, 8, 103, 0, 8, 39, 0, 9, 174, 0, 8, 7, 0, 8, 135, 0, 8, 71, 0, 9, 238, 80, 7, 9, 0, 8, 95, 0, 8, 31, 0, 9, 158, 84, 7, 99, 0, 8, 127, 0, 8, 63, 0, 9, 222, 82, 7, 27, 0, 8, 111, 0, 8, 47, 0, 9, 190, 0, 8, 15, 0, 8, 143, 0, 8, 79, 0, 9, 254, 96, 7, 256, 0, 8, 80, 0, 8, 16, 84, 8, 115, 82, 7, 31, 0, 8, 112, 0, 8, 48, 0, 9, 193, 80, 7, 10, 0, 8, 96, 0, 8, 32, 0, 9, 161, 0, 8, 0, 0, 8, 128, 0, 8, 64, 0, 9, 225, 80, 7, 6, 0, 8, 88, 0, 8, 24, 0, 9, 145, 83, 7, 59, 0, 8, 120, 0, 8, 56, 0, 9, 209, 81, 7, 17, 0, 8, 104, 0, 8, 40, 0, 9, 177, 0, 8, 8, 0, 8, 136, 0, 8, 72, 0, 9, 241, 80, 7, 4, 0, 8, 84, 0, 8, 20, 85, 8, 227, 83, 7, 43, 0, 8, 116, 0, 8, 52, 0, 9, 201, 81, 7, 13, 0, 8, 100, 0, 8, 36, 0, 9, 169, 0, 8, 4, 0, 8, 132, 0, 8, 68, 0, 9, 233, 80, 7, 8, 0, 8, 92, 0, 8, 28, 0, 9, 153, 84, 7, 83, 0, 8, 124, 0, 8, 60, 0, 9, 217, 82, 7, 23, 0, 8, 108, 0, 8, 44, 0, 9, 185, 0, 8, 12, 0, 8, 140, 0, 8, 76, 0, 9, 249, 80, 7, 3, 0, 8, 82, 0, 8, 18, 85, 8, 163, 83, 7, 35, 0, 8, 114, 0, 8, 50, 0, 9, 197, 81, 7, 11, 0, 8, 98, 0, 8, 34, 0, 9, 165, 0, 8, 2, 0, 8, 130, 0, 8, 66, 0, 9, 229, 80, 7, 7, 0, 8, 90, 0, 8, 26, 0, 9, 149, 84, 7, 67, 0, 8, 122, 0, 8, 58, 0, 9, 213, 82, 7, 19, 0, 8, 106, 0, 8, 42, 0, 9, 181, 0, 8, 10, 0, 8, 138, 0, 8, 74, 0, 9, 245, 80, 7, 5, 0, 8, 86, 0, 8, 22, 192, 8, 0, 83, 7, 51, 0, 8, 118, 0, 8, 54, 0, 9, 205, 81, 7, 15, 0, 8, 102, 0, 8, 38, 0, 9, 173, 0, 8, 6, 0, 8, 134, 0, 8, 70, 0, 9, 237, 80, 7, 9, 0, 8, 94, 0, 8, 30, 0, 9, 157, 84, 7, 99, 0, 8, 126, 0, 8, 62, 0, 9, 221, 82, 7, 27, 0, 8, 110, 0, 8, 46, 0, 9, 189, 0, 8,
                        14, 0, 8, 142, 0, 8, 78, 0, 9, 253, 96, 7, 256, 0, 8, 81, 0, 8, 17, 85, 8, 131, 82, 7, 31, 0, 8, 113, 0, 8, 49, 0, 9, 195, 80, 7, 10, 0, 8, 97, 0, 8, 33, 0, 9, 163, 0, 8, 1, 0, 8, 129, 0, 8, 65, 0, 9, 227, 80, 7, 6, 0, 8, 89, 0, 8, 25, 0, 9, 147, 83, 7, 59, 0, 8, 121, 0, 8, 57, 0, 9, 211, 81, 7, 17, 0, 8, 105, 0, 8, 41, 0, 9, 179, 0, 8, 9, 0, 8, 137, 0, 8, 73, 0, 9, 243, 80, 7, 4, 0, 8, 85, 0, 8, 21, 80, 8, 258, 83, 7, 43, 0, 8, 117, 0, 8, 53, 0, 9, 203, 81, 7, 13, 0, 8, 101, 0, 8, 37, 0, 9, 171, 0, 8, 5, 0, 8, 133, 0, 8, 69, 0, 9, 235, 80, 7, 8, 0, 8, 93, 0, 8, 29, 0, 9, 155, 84, 7, 83, 0, 8, 125, 0, 8, 61, 0, 9, 219, 82, 7, 23, 0, 8, 109, 0, 8, 45, 0, 9, 187, 0, 8, 13, 0, 8, 141, 0, 8, 77, 0, 9, 251, 80, 7, 3, 0, 8, 83, 0, 8, 19, 85, 8, 195, 83, 7, 35, 0, 8, 115, 0, 8, 51, 0, 9, 199, 81, 7, 11, 0, 8, 99, 0, 8, 35, 0, 9, 167, 0, 8, 3, 0, 8, 131, 0, 8, 67, 0, 9, 231, 80, 7, 7, 0, 8, 91, 0, 8, 27, 0, 9, 151, 84, 7, 67, 0, 8, 123, 0, 8, 59, 0, 9, 215, 82, 7, 19, 0, 8, 107, 0, 8, 43, 0, 9, 183, 0, 8, 11, 0, 8, 139, 0, 8, 75, 0, 9, 247, 80, 7, 5, 0, 8, 87, 0, 8, 23, 192, 8, 0, 83, 7, 51, 0, 8, 119, 0, 8, 55, 0, 9, 207, 81, 7, 15, 0, 8, 103, 0, 8, 39, 0, 9, 175, 0, 8, 7, 0, 8, 135, 0, 8, 71, 0, 9, 239, 80, 7, 9, 0, 8, 95, 0, 8, 31, 0, 9, 159, 84, 7, 99, 0, 8, 127, 0, 8, 63, 0, 9, 223, 82, 7, 27, 0, 8, 111, 0, 8, 47, 0, 9, 191, 0, 8, 15, 0, 8, 143, 0, 8, 79, 0, 9, 255};
        //UPGRADE_NOTE: Final was removed from the declaration of 'fixed_td'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] fixed_td = new int[] { 80, 5, 1, 87, 5, 257, 83, 5, 17, 91, 5, 4097, 81, 5, 5, 89, 5, 1025, 85, 5, 65, 93, 5, 16385, 80, 5, 3, 88, 5, 513, 84, 5, 33, 92, 5, 8193, 82, 5, 9, 90, 5, 2049, 86, 5, 129, 192, 5, 24577, 80, 5, 2, 87, 5, 385, 83, 5, 25, 91, 5, 6145, 81, 5, 7, 89, 5, 1537, 85, 5, 97, 93, 5, 24577, 80, 5, 4, 88, 5, 769, 84, 5, 49, 92, 5, 12289, 82, 5, 13, 90, 5, 3073, 86, 5, 193, 192, 5, 24577 };

        // Tables for deflate from PKZIP's appnote.txt.
        //UPGRADE_NOTE: Final was removed from the declaration of 'cplens'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] cplens = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0 };

        // see note #13 above about 258
        //UPGRADE_NOTE: Final was removed from the declaration of 'cplext'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] cplext = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, 112, 112 };

        //UPGRADE_NOTE: Final was removed from the declaration of 'cpdist'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] cpdist = new int[] { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };

        //UPGRADE_NOTE: Final was removed from the declaration of 'cpdext'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] cpdext = new int[] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

        // If BMAX needs to be larger than 16, then h and bitOffset[] should be uLong.
        private const int BMAX = 15; // maximum bit length of any code

        private int[] hn = null; // hufts used in space
        private int[] huftBuildWorkArea = null; // work area for huft_build 
        private int[] bitLenghtCountTable = null; // bit length count table
        private int[] structureAssignmentTable = null; // table entry for structure assignment
        private int[] stackTable = null; // table stack
        private int[] bitOffset = null; // bit offsets, then code stack

        private int huft_build(int[] b, int bindex, int n, int s, int[] d, int[] e, int[] t, int[] m, int[] hp, int[] hn, int[] v)
        {
            // Given a list of code lengths and a maximum table size, make a set of
            // tables to decode that set of codes.  Return Z_OK on success, Z_BUF_ERROR
            // if the given code set is incomplete (the tables are still built in this
            // case), Z_DATA_ERROR if the input is invalid (an over-subscribed set of
            // lengths), or Z_MEM_ERROR if not enough memory.


            // Generate counts for each bit length

            int pointer = 0;// pointer into c[], b[], or huftBuildWorkArea[]
            int iCounter = n;// counter, current code
            do
            {
                bitLenghtCountTable[b[bindex + pointer]]++; pointer++; iCounter--; // assume all entries <= BMAX
            }
            while (iCounter != 0);

            if (bitLenghtCountTable[0] == n)
            {
                // null input--all zero length codes
                t[0] = -1;
                m[0] = 0;
                return Z_OK;
            }

            // Find minimum and maximum length, bound *m by those
            int bitsPerTable = m[0]; // bits per table (returned in m)
            int jCounter; // counter
            for (jCounter = 1; jCounter <= BMAX; jCounter++)
                if (bitLenghtCountTable[jCounter] != 0)
                    break;
            int bitsNumberInCurrentCode = jCounter; // number of bits in current code
            if (bitsPerTable < jCounter)
            {
                bitsPerTable = jCounter;
            }
            for (iCounter = BMAX; iCounter != 0; iCounter--)
            {
                if (bitLenghtCountTable[iCounter] != 0)
                    break;
            }
            int maxCodeLength = iCounter; // maximum code length
            if (bitsPerTable > iCounter)
            {
                bitsPerTable = iCounter;
            }
            m[0] = bitsPerTable;

            // Adjust last length count to fill out codes, if needed
            int y; // number of dummy codes added
            for (y = 1 << jCounter; jCounter < iCounter; jCounter++, y <<= 1)
            {
                if ((y -= bitLenghtCountTable[jCounter]) < 0)
                {
                    return Z_DATA_ERROR;
                }
            }
            if ((y -= bitLenghtCountTable[iCounter]) < 0)
            {
                return Z_DATA_ERROR;
            }
            bitLenghtCountTable[iCounter] += y;

            // Generate starting offsets into the value table for each length
            bitOffset[1] = jCounter = 0;
            pointer = 1;
            int xp = 2;  // pointer into bitOffset
            while (--iCounter != 0)
            {
                // note that i == g from above
                bitOffset[xp] = (jCounter += bitLenghtCountTable[pointer]);
                xp++;
                pointer++;
            }

            // Make a table of values in order of bit lengths
            iCounter = 0; pointer = 0;
            do
            {
                if ((jCounter = b[bindex + pointer]) != 0)
                {
                    v[bitOffset[jCounter]++] = iCounter;
                }
                pointer++;
            }
            while (++iCounter < n);
            n = bitOffset[maxCodeLength]; // set n to length of huftBuildWorkArea

            // Generate the Huffman codes and for each, make the table entries
            bitOffset[0] = iCounter = 0; // first Huffman code is zero
            pointer = 0; // grab values in bit order
            int tableLevel = -1; // table level
            int bitsBeforeThisTable = -bitsPerTable; // bits before this table == (l * h)
            stackTable[0] = 0; // just to keep compilers happy
            int pointerToCurrentTable = 0;  // points to current table
            int numberOfEnteriesInCurrentTable = 0;  // number of entries in current table

            // go through the bit lengths (k already is bits in shortest code)
            for (; bitsNumberInCurrentCode <= maxCodeLength; bitsNumberInCurrentCode++)
            {
                int a = bitLenghtCountTable[bitsNumberInCurrentCode];  // counter for codes of length k
                while (a-- != 0)
                {
                    // here i is the Huffman code of length k bits for value *p
                    // make tables up to required level
                    int f; // i repeats in table every f entries
                    while (bitsNumberInCurrentCode > bitsBeforeThisTable + bitsPerTable)
                    {
                        tableLevel++;
                        bitsBeforeThisTable += bitsPerTable; // previous table always l bits
                                           // compute minimum size table less than or equal to l bits
                        numberOfEnteriesInCurrentTable = maxCodeLength - bitsBeforeThisTable;
                        numberOfEnteriesInCurrentTable = (numberOfEnteriesInCurrentTable > bitsPerTable) ? bitsPerTable : numberOfEnteriesInCurrentTable; // table size upper limit
                        if ((f = 1 << (jCounter = bitsNumberInCurrentCode - bitsBeforeThisTable)) > a + 1)
                        {
                            // try a k-w bit table
                            // too few codes for k-w bit table
                            f -= (a + 1); // deduct codes from patterns left
                            xp = bitsNumberInCurrentCode;
                            if (jCounter < numberOfEnteriesInCurrentTable)
                            {
                                while (++jCounter < numberOfEnteriesInCurrentTable)
                                {
                                    // try smaller tables up to z bits
                                    if ((f <<= 1) <= bitLenghtCountTable[++xp])
                                        break; // enough codes to use up j bits
                                    f -= bitLenghtCountTable[xp]; // else deduct codes from patterns
                                }
                            }
                        }
                        numberOfEnteriesInCurrentTable = 1 << jCounter; // table entries for j-bit table

                        // allocate new table
                        if (hn[0] + numberOfEnteriesInCurrentTable > MANY)
                        {
                            // (note: doesn't matter for fixed)
                            return Z_DATA_ERROR; // overflow of MANY
                        }
                        stackTable[tableLevel] = pointerToCurrentTable = hn[0]; // DEBUG
                        hn[0] += numberOfEnteriesInCurrentTable;

                        // connect to last table, if there is one
                        if (tableLevel != 0)
                        {
                            bitOffset[tableLevel] = iCounter; // save pattern for backing up
                            structureAssignmentTable[0] = (sbyte)jCounter; // bits in this table
                            structureAssignmentTable[1] = (sbyte)bitsPerTable; // bits to dump before this table
                            jCounter = SharedUtils.URShift(iCounter, (bitsBeforeThisTable - bitsPerTable));
                            structureAssignmentTable[2] = (int)(pointerToCurrentTable - stackTable[tableLevel - 1] - jCounter); // offset to this table
                            Array.Copy(structureAssignmentTable, 0, hp, (stackTable[tableLevel - 1] + jCounter) * 3, 3); // connect to last table
                        }
                        else
                        {
                            t[0] = pointerToCurrentTable; // first table is returned result
                        }
                    }

                    // set up table entry in structureAssignmentTable
                    structureAssignmentTable[1] = (sbyte)(bitsNumberInCurrentCode - bitsBeforeThisTable);
                    if (pointer >= n)
                    {
                        structureAssignmentTable[0] = 128 + 64; // out of values--invalid code
                    }
                    else if (v[pointer] < s)
                    {
                        structureAssignmentTable[0] = (sbyte)(v[pointer] < 256 ? 0 : 32 + 64); // 256 is end-of-block
                        structureAssignmentTable[2] = v[pointer++]; // simple code is just the value
                    }
                    else
                    {
                        structureAssignmentTable[0] = (sbyte)(e[v[pointer] - s] + 16 + 64); // non-simple--look up in lists
                        structureAssignmentTable[2] = d[v[pointer++] - s];
                    }

                    // fill code-like entries with structureAssignmentTable
                    f = 1 << (bitsNumberInCurrentCode - bitsBeforeThisTable);
                    for (jCounter = SharedUtils.URShift(iCounter, bitsBeforeThisTable); jCounter < numberOfEnteriesInCurrentTable; jCounter += f)
                    {
                        Array.Copy(structureAssignmentTable, 0, hp, (pointerToCurrentTable + jCounter) * 3, 3);
                    }

                    // backwards increment the k-bit code i
                    for (jCounter = 1 << (bitsNumberInCurrentCode - 1); (iCounter & jCounter) != 0; jCounter = SharedUtils.URShift(jCounter, 1))
                    {
                        iCounter ^= jCounter;
                    }
                    iCounter ^= jCounter;

                    // backup over finished tables
                    int mask = (1 << bitsBeforeThisTable) - 1;  // (1 << w) - 1, to avoid cc -O bug on HP
                    while ((iCounter & mask) != bitOffset[tableLevel])
                    {
                        tableLevel--; // don't need to update q
                        bitsBeforeThisTable -= bitsPerTable;
                        mask = (1 << bitsBeforeThisTable) - 1;
                    }
                }
            }
            // Return Z_BUF_ERROR if we were given an incomplete table
            return y != 0 && maxCodeLength != 1 ? Z_BUF_ERROR : Z_OK;
        }

        internal int inflate_trees_bits(int[] c, int[] bb, int[] tb, int[] hp, ZlibCodec z)
        {
            int result;
            initWorkArea(19);
            hn[0] = 0;
            result = huft_build(c, 0, 19, 19, null, null, tb, bb, hp, hn, huftBuildWorkArea);

            if (result == Z_DATA_ERROR)
            {
                z.Message = "oversubscribed dynamic bit lengths tree";
            }
            else if (result == Z_BUF_ERROR || bb[0] == 0)
            {
                z.Message = "incomplete dynamic bit lengths tree";
                result = Z_DATA_ERROR;
            }
            return result;
        }

        internal int inflate_trees_dynamic(int nl, int nd, int[] c, int[] bl, int[] bd, int[] tl, int[] td, int[] hp, ZlibCodec z)
        {
            int result;

            // build literal/length tree
            initWorkArea(288);
            hn[0] = 0;
            result = huft_build(c, 0, nl, 257, cplens, cplext, tl, bl, hp, hn, huftBuildWorkArea);
            if (result != Z_OK || bl[0] == 0)
            {
                if (result == Z_DATA_ERROR)
                {
                    z.Message = "oversubscribed literal/length tree";
                }
                else if (result != Z_MEM_ERROR)
                {
                    z.Message = "incomplete literal/length tree";
                    result = Z_DATA_ERROR;
                }
                return result;
            }

            // build distance tree
            initWorkArea(288);
            result = huft_build(c, nl, nd, 0, cpdist, cpdext, td, bd, hp, hn, huftBuildWorkArea);

            if (result != Z_OK || (bd[0] == 0 && nl > 257))
            {
                if (result == Z_DATA_ERROR)
                {
                    z.Message = "oversubscribed distance tree";
                }
                else if (result == Z_BUF_ERROR)
                {
                    z.Message = "incomplete distance tree";
                    result = Z_DATA_ERROR;
                }
                else if (result != Z_MEM_ERROR)
                {
                    z.Message = "empty distance tree with lengths";
                    result = Z_DATA_ERROR;
                }
                return result;
            }

            return Z_OK;
        }

        internal static int inflate_trees_fixed(int[] bl, int[] bd, int[][] tl, int[][] td, ZlibCodec z)
        {
            bl[0] = fixed_bl;
            bd[0] = fixed_bd;
            tl[0] = fixed_tl;
            td[0] = fixed_td;
            return Z_OK;
        }

        private void initWorkArea(int vsize)
        {
            if (hn == null)
            {
                hn = new int[1];
                huftBuildWorkArea = new int[vsize];
                bitLenghtCountTable = new int[BMAX + 1];
                structureAssignmentTable = new int[3];
                stackTable = new int[BMAX];
                bitOffset = new int[BMAX + 1];
            }
            else
            {
                if (huftBuildWorkArea.Length < vsize)
                {
                    huftBuildWorkArea = new int[vsize];
                }
                Array.Clear(huftBuildWorkArea, 0, vsize);
                Array.Clear(bitLenghtCountTable, 0, BMAX + 1);
                structureAssignmentTable[0] = 0; structureAssignmentTable[1] = 0; structureAssignmentTable[2] = 0;
                //  for(int i=0; i<BMAX; i++){stackTable[i]=0;}
                //Array.Copy(c, 0, stackTable, 0, BMAX);
                Array.Clear(stackTable, 0, BMAX);
                //  for(int i=0; i<BMAX+1; i++){bitOffset[i]=0;}
                //Array.Copy(c, 0, bitOffset, 0, BMAX + 1);
                Array.Clear(bitOffset, 0, BMAX + 1);
            }
        }
    }
}