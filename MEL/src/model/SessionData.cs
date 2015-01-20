/**
 * Kinematic Template Matching
 *
 *      Phillip T. Pasqual {phillpas@uw.edu}
 *		Jacob O. Wobbrock, Ph.D. {wobbrock@uw.edu}
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2014, Phillip T. Pasqual. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington nor the names of its 
 *      contributors may be used to endorse or promote products derived from 
 *      this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
**/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Diagnostics;
using WobbrockLib;
using WobbrockLib.Extensions;
using System.Linq;

namespace KTM
{
    /// <summary>
    /// This class encapsulates a single KTM session for either collecting training data or testing
    /// a model.Conditions comprise a session, trials comprise a condition, and movement comprises a trial.
    /// </summary>
    public class SessionData
    {
        #region Fields

        private int _subjNum;
        private string _gender;
        private int _age;
        private int _minA;
        private int _maxA;
        private int _w;
        private int _numTrials;
        private bool _is1D;
        private List<BlockData> _blocks;
        private int curBlockIndex;
       
        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a KTM instance. An KTM session contains blocks for data collection or testing,
        /// which, in turn, contain a set of trials. A constructed instance contains a list of blocks 
        /// in sequence, which themselves contain a list of trials.
        /// </summary>
        /// <param name="subjNum">The subject's ID that is taking part in this session</param>
        /// <param name="gender">The gender of the current subject</param>
        /// <param name="age">The age of the subject</param>
        /// <param name="minA">The minimum target amplitude (distance between targets) that will be used in pixels</param>
        /// <param name="maxA">The maximum target amplitude (distance between targets) that will be used in piexls</param>
        /// <param name="w">The width of the targets in pixels</param>
        /// <param name="numTrials">The number of trials per block</param>
        /// <param name="numBlocks">The number of blocks in this session</param>
        /// <param name="is1D">True if this session is using the vertial ribbon targets, False otherwise</param>
        public SessionData(int subjNum, string gender, int age, int minA, int maxA, int w, int numTrials, int numBlocks, bool is1D)
        {
            this._subjNum = subjNum;
            this._gender = gender;
            this._age = age;
            this._minA = minA;
            this._maxA = maxA;
            this._w = w;
            this._numTrials = numTrials;
            this._is1D = is1D;

            this._blocks = new List<BlockData>();

            for(int b = 0; b < numBlocks; b++)
            {
                BlockData bd = new BlockData(b, minA, maxA, w, numTrials, is1D);
                _blocks.Add(bd);
            }

            this.curBlockIndex = 0;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the condition at the specified index.
        /// </summary>
        /// <param name="block">The 0-based block index. A block is a repeat of all conditions in the same order.</param>
        /// <param name="index">The 0-based condition index</param>
        /// <returns>The condition at the given index, or <b>null</b> if it does not exist.</returns>
        public List<BlockData> Blocks
        {
            get { return this._blocks; }
        }

        public int NumBlocks
        {
            get
            {
                // the block index of the last condition, plus one, should be the number of
                // blocks, as all blocks have the same number of conditions.
                return _blocks.Count;
            }
        }

        public int SubjNum
        {
            get { return _subjNum; }
        }

        public int CurBlockIndex
        {
            get { return this.curBlockIndex; }
            set { this.curBlockIndex = value; }
        }

        #endregion

        #region Filename

        /// <summary>
        /// Gets the filename base for this session. The filename base currently
        /// shows only the subject ID
        /// </summary>
        public string FilenameBase
        {
            get
            {
                return String.Format("s{0}",
                    _subjNum > 9 ? _subjNum.ToString() : "0" + _subjNum);
            }
        }

        #endregion

        /// <summary>
        /// Logs all mouse movement data for this session to a file in the given directory
        /// </summary>
        /// <param name="dirName">the directory to log the data</param>
        public void logAll(string dirName)
        {
            System.IO.Directory.CreateDirectory(dirName);
            string fileName = "Log";
            if (_is1D)
                fileName += "_1D.csv";
            else
                fileName += "_2D.csv";

            StreamWriter csvWriter = new StreamWriter(dirName + fileName, false, Encoding.Default);
            
            //write the first line containing headers
            csvWriter.WriteLine("ID,X,Y,T,isError?,targX,targY");

            int pathID = 0;
            for(int b = 0; b < this.NumBlocks; b++)
            {
                BlockData bd = _blocks[b];

                for (int j = 1; j <= bd.NumTrials; j++)
                {
                    TrialData td = bd[j];
                    List<TimePointF> moves = td.Moves;

                    //output data to files    
                    for (int k = 0; k < moves.Count; k++)
                    {
                        TimePointF point = moves[k];
                        RectangleF trect = td.TargetBounds;
                        csvWriter.WriteLine(pathID + "," + point.X + "," + point.Y + "," + point.Time + "," + td.IsError.ToString() + "," + (trect.X + trect.Width / 2) + "," + (trect.Y + trect.Width / 2));
                    }
                    pathID++;
                } 
            }
            csvWriter.Close();
        }
    }
}
