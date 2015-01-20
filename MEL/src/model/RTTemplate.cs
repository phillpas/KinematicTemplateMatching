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
using WobbrockLib;
using WobbrockLib.Extensions;
using System.Drawing;
using System.Diagnostics;

namespace KTM
{
    /// <summary>
    /// Real Time Template to be used when predictions need to be made in real time
    /// </summary>
    class RTTemplate
    {
        private int idx = 0;

        private const int Hertz = 20; //The frequency in cycles/second at which to resample.
        private float dist_crow; //"as the crow flies" distance of this template's path
        private List<TimePointF> raw_points; //all raw points that make up this template's path as read in by the log file
        private List<PointF> resampled_vel; //contains the temporally resampled VELOCITY points making up the ENTIRE path
        private List<PointF> smoothed_vel; //the smoothed velocity points using the complete list of raw points
        private int cur_time = 0;
        private List<string> predData;

        /// <summary>
        /// The standard deviation of the Gaussian kernel to use. The greater the standard deviation, 
        /// the larger the kernel, and the smoother the result, as more neighboring values are taken into 
        /// account when computing the current value. The two-sided kernel will be of size
        /// 3 * <i>stdev</i> * 2 + 1, which means each smoothed value takes this many resampled values into
        /// account according to the weighting given by the kernel at each position.
        /// </summary>
        private const int GaussianStdDev = 7; // 3*(5)*2+1 = 31 kernel size.

        /// <summary>
        /// The weighting kernel to be used as a 1D convolution filter. The kernel size is based on
        /// the standard deviation of a Gaussian curve, which reaches zero at about 3 times this
        /// value.
        /// </summary>
        private readonly double[] Kernel;

        public RTTemplate()
        {
            this.raw_points = new List<TimePointF>();
            Kernel = SeriesEx.GaussianKernel(GaussianStdDev);
            this.predData = new List<string>();
        }

        public List<TimePointF> RawPoints
        {
            get { return this.raw_points; }
        }

        public int IDX
        {
            get { return this.idx; }
        }

        public int CurTime
        {
            get { return this.cur_time; }
        }

        public float DistCrow
        {
            get { return this.dist_crow; }
        }

        public List<string> PredData
        {
            get { return this.predData; }
        }

        public void AddPoint(TimePointF point)
        {
            this.raw_points.Add(point);
            List<TimePointF> temp = SeriesEx.ResampleInTime(this.raw_points, Hertz);
            this.resampled_vel = SeriesEx.Derivative(temp);
            this.smoothed_vel = SeriesEx.Filter(this.resampled_vel, Kernel);

            this.dist_crow = CalcCrowDist(this.raw_points);
        }


        /// <summary>
        /// calculates the as-the-crow-flies distance
        /// of the given points
        /// </summary>
        /// <param name="points">the points to calculate over</param>
        /// <returns>the as-the-crow-flies distance</returns>
        private float CalcCrowDist(List<TimePointF> points)
        {
            float x1 = points[0].X;
            float y1 = points[0].Y;
            float x2 = points.Last().X;
            float y2 = points.Last().Y;
            return (float)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        public void Clear()
        {
            this.raw_points.Clear();
            if(this.resampled_vel != null) this.resampled_vel.Clear();
            if(this.smoothed_vel != null) this.smoothed_vel.Clear();
            predData.Clear();
            idx++;
        }

        public TimePointF Predict1D(TemplateLibrary lib)
        {
            Template win_temp = lib[0];
            int win_id = 0;
            float win_score = float.MaxValue;
            for (int tempIndex = 0; tempIndex < lib.Library.Count; tempIndex++)
            {
                float score = this.CompareTo(lib[tempIndex]);
                if (score < win_score)
                {
                    win_score = score;
                    win_temp = lib[tempIndex];
                    win_id = tempIndex;
                }
            }

            //get the predicted distance
            float dist = win_temp.DistCrow;

            if (this.raw_points.Count > 1)
            {
                if (this.raw_points[this.raw_points.Count - 1].X < this.raw_points[0].X)
                    dist *= -1;
            }

            predData.Add(this.raw_points.Count + "," + win_id + "," + raw_points.Last().X + "," + raw_points.Last().Y + "," + this.resampled_vel.Last().X + "," + (raw_points[0].X + dist) + ",0," + dist);

            return new TimePointF(raw_points[0].X + dist, raw_points[0].Y, this.resampled_vel.Last().X);
        }

        public TimePointF Predict2D(TemplateLibrary lib)
        {
            Template win_temp = lib[0];
            int win_id = 0;
            float win_score = float.MaxValue;
            for (int tempIndex = 0; tempIndex < lib.Library.Count; tempIndex++)
            {
                float score = this.CompareTo(lib[tempIndex]);
                if (score < win_score)
                {
                    win_score = score;
                    win_temp = lib[tempIndex];
                    win_id = tempIndex;
                }
            }

            //get the predicted distance
            float dist = win_temp.DistCrow;

            TimePointF predPoint = new TimePointF(0, 0, 0);

            if (this.raw_points.Count > 1)
            {
                double radians = GeotrigEx.Angle(raw_points[0], raw_points.Last(), false);
                float x = raw_points[0].X + (float)(Math.Cos(radians) * dist);
                float y = raw_points[0].Y + (float)(Math.Sin(radians) * dist);
                predPoint = new TimePointF(x, y, this.resampled_vel.Last().X);
            }

            predData.Add(this.raw_points.Count + "," + win_id + "," + raw_points.Last().X + "," + raw_points.Last().Y + "," + this.resampled_vel.Last().X + "," + predPoint.X + "," + predPoint.Y + "," + dist);

            return predPoint;
        }

        private float CompareTo(Template other)
        {
            //get a sublist of "other's" resampled velocity points
            List<PointF> other_vel_sub = new List<PointF>();
            for (int i = 0; i < this.resampled_vel.Count; i++)
            {
                if (i < other.ResampledVel.Count)
                    other_vel_sub.Add(other.ResampledVel[i]);
            }

            //smooth points
            List<PointF> other_smooth_vel = SeriesEx.Filter(other_vel_sub, Kernel);

            //compare
            float score = 0f;
            for (int i = 0; i < this.smoothed_vel.Count; i++)
            {
                if (i < other_smooth_vel.Count)
                    score += Math.Abs(this.smoothed_vel[i].Y - other_smooth_vel[i].Y);
                else
                    score += this.smoothed_vel[i].Y;
            }
            score /= (float)this.smoothed_vel.Count;
            return score;
        }
    }
}
