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
    public class TrialData1D : TrialData
    {
        #region Fields

        private RectangleF _thisRect;
        private RectangleF _lastRect;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for trial. 
        /// </summary>
        /// <param name="index">The 0-based index number of this trial.</param>
        /// <param name="isTraining">True if this data from this trial will be used as training data, false otherwise</param>
        /// <param name="thisCircle">The target that the user should be moving towards during this trial</param>
        /// <param name="lastCircle">The target that the user clicked on to end the last trial and start this trial</param>
        public TrialData1D(int index, RectangleF thisRect, RectangleF lastRect, double A)
            : base(index, A)
        {
            _thisRect = thisRect;
            _lastRect = lastRect;
        }


        #endregion

        /// <summary>
        /// Gets a paintable region object representing this target. Because it is a 
        /// GDI+ resource, the region should be disposed of after being used.
        /// </summary>
        public override Region TargetRegion
        {
            get
            {
                return new Region(_thisRect);
            }
        }


        /// <summary>
        /// Gets the bounding rectangle for this target.
        /// </summary>
        public override RectangleF TargetBounds
        {
            get { return _thisRect; }
        }

        /// <summary>
        /// Tests whether or not the point supplied is contained within the target.
        /// </summary>
        /// <param name="pt">The point to test.</param>
        /// <returns>True if the point is contained; false otherwise.</returns>
        /// <remarks>Note that is is not sufficient to use the <b>FittsTrial.TargetBounds</b> 
        /// property to hit-test the point, since not all targets are rectangular in shape.</remarks>
        public override bool TargetContains(PointF pt)
        {
            return _thisRect.Contains(pt);
        }

        public override PointF TargetCenter
        {
            get { return new PointF(_thisRect.Left + _thisRect.Width / 2f, _thisRect.Top + _thisRect.Height / 2f); }
        }

        //gets the current prediction of the endpoint
        public override PointF Prediction()
        {
                return PointF.Empty;
        }

    }
}