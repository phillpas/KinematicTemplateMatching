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
using System.Linq;
using System.Text;
using System.Drawing;
using WobbrockLib;

namespace KTM
{
    public class BlockData
    {
        private int _blockNum;
        private List<TrialData> _trials;
        private int _w;
        private int _numTrials;
        private bool _is1D;
        private List<RectangleF> _rects;
        private List<CircleF> _circles;
        List<double> _aVals;
        private static Random rand = new Random((int)DateTime.Now.Ticks);

        #region Properties

        public int BlockNum
        {
            get { return _blockNum; }
        }

        public int W
        {
            get { return _w; }
        }

        #endregion


        public BlockData(int blockNum, int minA, int maxA, int W, int numTrials, bool is1D)
        {
            this._blockNum = blockNum;
            this._w = W;
            this._numTrials = numTrials;
            this._is1D = is1D;

            //get the bounding rectangle of the main canvas
            System.Windows.Size bounds = MainWindow.mainWindow.mainCanvas.RenderSize;
            PointF center = new PointF((float)bounds.Width / 2f, (float)bounds.Height / 2f);
            this._aVals = new List<double>();

            if (is1D)
            {
                //add the start rect
                this._rects = new List<RectangleF>();
                _rects.Add(new RectangleF(center.X - W / 2f, 0f, W, (float)bounds.Height));

                //add the actual target Rectangles
                float lastCX = center.X - W / 2f;
                for (int i = 0; i <= numTrials; i++)
                {
                    bool foundA = false;
                    int A = rand.Next(minA, maxA);
                    int j;
                    if (lastCX + A + W + 20 < bounds.Width - lastCX)
                    {
                        j = 1;
                        foundA = true;
                    }
                    else if (lastCX - A - W - 20 > 0)
                    {
                        j = -1;
                        foundA = true;
                    }
                    else
                    {
                        j = 0;
                        i--;
                    }
                    if (foundA)
                    {
                        float cx = lastCX + (j * A) - W / 2f;
                        _rects.Add(new RectangleF(cx, 0f, W, (float)bounds.Height));
                        _aVals.Add(A);
                        lastCX = cx;
                    }
                }
            }
            else
            {
                double radians = 3.0 * Math.PI / 2.0; //start from the top (270 degrees)
                double delta = (2.0 * Math.PI) / _numTrials; //radian delta between circles

                CircleF[] temp = new CircleF[_numTrials + 1]; //add 1 for special start area trial at index 0
                for (int i = 0; i < temp.Length; i++)
                {
                    int A = rand.Next(minA, maxA);
                    float x = center.X + (float)(Math.Cos(radians) * (A / 2.0));
                    float y = center.Y + (float)(Math.Sin(radians) * (A / 2.0));
                    temp[i] = new CircleF(x, y, W / 2f);
                    radians += delta;
                }

                // order the targets appropriately according to the ISO 9241-9 standard
                CircleF[] circs = new CircleF[temp.Length];
                for (int i = 0, j = 0; i < (int)Math.Ceiling(circs.Length / 2f); i++, j += 2) // even slots
                    circs[j] = temp[i];
                for (int i = (int)Math.Ceiling(circs.Length / 2f), j = 1; i < circs.Length; i++, j += 2) // odd slots
                    circs[j] = temp[i];

                _circles = circs.ToList<CircleF>();

                //calculate the a values based on the layout
                _aVals.Add(0);//the distance to the first circle. arbitrary value
                for(int i = 0; i < _circles.Count - 1; i++)
                {
                    PointF p1 = new PointF(_circles[i].X,_circles[i].Y);
                    PointF p2 = new PointF(_circles[i+1].X,_circles[i+1].Y);
                    _aVals.Add((int)WobbrockLib.Extensions.GeotrigEx.Distance(p1, p2));
                }
            }

            foreach (int aval in _aVals)
                Console.WriteLine(aval);

            createTrials(is1D);
        }

        /// <summary>
        // Create the trial instances that represent the trials with the given targets. This includes
        // creating the special start-area trial at index 0 in the condition representing the starting
        // area that, when clicked, starts the actual trials.
        /// </summary>
        private void createTrials(bool is1D)
        {
            _trials = new List<TrialData>(_numTrials);
            for (int i = 0; i <= _numTrials; i++)
            {
                TrialData td;
                if (is1D)
                    td = new TrialData1D(i, _rects[i], i == 0 ? RectangleF.Empty : _rects[i - 1], _aVals[i]);
                else
                    td = new TrialData2D(i, _circles[i], i == 0 ? CircleF.Empty : _circles[i - 1], _aVals[i]);
                _trials.Add(td);
            }
        }

        /// <summary>
        /// Gets the trial instance at the given index number, or <b>null</b> if none exists.
        /// </summary>
        /// <param name="number">The 1-based number of the trial to get. There is a trial instance
        /// at index 0, but this is not a real trial, per se, but a special trial for indicating
        /// the start target area for the condition. Thus, to retrieve trial number N, pass in index
        /// N as the number to this accessor.</param>
        /// <returns>The FittsTrial instance at the given index number.</returns>
        public TrialData this[int number]
        {
            get
            {
                if (0 <= number && number < _trials.Count)
                {
                    return _trials[number];
                }
                return null;
            }
        }

        /// <summary>
        /// Clears all the trial data currently stored by this condition, if any. Does not clear
        /// the trial condition values that specify the trials.
        /// </summary>
        public void ClearTrialData()
        {
            for (int i = 1; i < _trials.Count; i++)
            {
                _trials[i].ClearData();
            }
        }

        /// <summary>
        /// Gets the number of trials in this condition. This does NOT include the special start 
        /// area trial occupying index 0. Thus, iterations over the number of trials should be
        /// from 1 to <i>NumTrials</i>, inclusive.
        /// </summary>
        public int NumTrials
        {
            get
            {
                return _trials.Count - 1; // don't count index 0
            }
        }



        /// <summary>
        /// Gets the number of completed trials in this condition. This does not include the special start 
        /// area trial occupying index 0.
        /// </summary>
        public int NumCompletedTrials
        {
            get
            {
                int num = 0;
                for (int i = 1; i < _trials.Count; i++) // start past the start area trial
                {
                    if (_trials[i].IsComplete)
                        num++;
                }
                return num;
            }
        }



        /// <summary>
        /// Gets the set of target regions that exist as part of this condition. There will be
        /// one region for every trial in the condition. Many of the regions may be identical.
        /// Because they are GDI+ resources, the regions should be disposed of after being used.
        /// </summary>
        public Region[] TargetRegions
        {
            get
            {
                Region[] rgns = new Region[_trials.Count];
                for (int i = 0; i < rgns.Length; i++) // get all regions, including the start-area one at index 0
                {
                    rgns[i] = _trials[i].TargetRegion;
                }
                return rgns;
            }
        }
    }
}
