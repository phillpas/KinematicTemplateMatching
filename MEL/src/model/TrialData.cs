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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Xml;
using WobbrockLib;
using WobbrockLib.Extensions;
using System.Linq;

namespace KTM
{
    /// <summary>
    /// This class encapsulates the data associated with a single trial (single click) within a
    /// condition within a KTM session. The class holds all information necessary for defining
    /// a single trial, including its target locations.
    /// </summary>
    public abstract class TrialData
    {
        #region Fields

        protected int _number; // 1-based number of this trial; trial 0 is reserved for the start area for the condition
        protected bool _isError;

        protected TimePointF _start; // the click point that started this trial
        protected TimePointF _end; // the click point that ended this trial

        protected List<TimePointF> _moves; //contains all of the movement points that make up this trial
        protected List<PointF> _predictions; //contains all of the predicted endpoints for this trial

        protected double _a; //target amplitude

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for trial. 
        /// </summary>
        /// <param name="index">The 0-based index number of this trial.</param>
        /// <param name="isTraining">True if this data from this trial will be used as training data, false otherwise</param>
        /// <param name="A">The A (amplitude) value to use for this trial</param>
        public TrialData(int index, double A)
        {
            _number = index;
            _isError = false;
            _start = TimePointF.Empty;
            _end = TimePointF.Empty;
            _moves = new List<TimePointF>();
            _predictions = new List<PointF>();
            _a = A;
        }

        #endregion

        #region Properties

        public double A
        {
            get { return _a; }
        }

        public List<TimePointF> Moves
        {
            get { return _moves; }
        }

        /// <summary>
        /// Gets the trial number of this trial. This is a 1-based index within the
        /// condition although there <i>is</i> a trial at index 0, which is the special 
        /// start-area trial.
        /// </summary>
        public int Number
        {
            get { return _number; }
        }

        /// <summary>
        /// Gets whether or not this trial is the special start-area trial, and therefore not
        /// really a trial at all, but a location for the initial click to begin a condition.
        /// </summary>
        public bool IsStartAreaTrial
        {
            get { return _number == 0; }
        }

        public bool IsError
        {
            get { return _isError; }
            set { _isError = value; }
        }

        //gets the current prediction of the endpoint
        public abstract PointF Prediction();

        public List<PointF> Predictions
        {
            get { return _predictions; }
        }

        public abstract Region TargetRegion
        {
            get;
        }

        public abstract RectangleF TargetBounds
        {
            get;
        }

        public abstract bool TargetContains(PointF pt);

        public abstract PointF TargetCenter
        {
            get;
        }

        #endregion

        #region

        /// <summary>
        /// Adds a new movement point to the list of all movement points
        /// that make up this trial if it is different from the last point. 
        /// Returns true if the point was added
        /// </summary>
        /// <param name="pt">The new movement point to add</param>
        public bool AddMove(TimePointF pt)
        {
            if (_moves.Count > 0)
            {
                TimePointF lastPt = _moves[_moves.Count - 1];
                if (GeotrigEx.Distance(lastPt, pt) >= 1.0f && pt.Time - lastPt.Time > 0f)
                {
                    _moves.Add(pt);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                _moves.Add(pt);
                return true;
            }
        }

        /// <summary>
        /// Clears the performance data associated with this trial. Does not clear the
        /// independent variables that define the settings for this trial.
        /// </summary>
        public virtual void ClearData()
        {
            _start = TimePointF.Empty;
            _end = TimePointF.Empty;
            _moves.Clear();
        }

        /// <summary>
        /// Gets whether or not this trial has been completed. A completed trial has been
        /// performed and therefore has a non-zero ending time-stamp.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return (_end.Time != 0L);
            }
        }

        /// <summary>
        /// Gets or sets the start click point and time that began this trial.
        /// </summary>
        public TimePointF Start
        {
            get { return _start; }
            set { _start = value; }
        }

        /// <summary>
        /// Gets or sets the selection endpoint and time that ended this trial.
        /// </summary>
        public TimePointF End
        {
            get { return _end; }
            set
            {
                _end = value;
                AddMove(_end);
            }
        }

        #endregion

    }
}