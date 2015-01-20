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
using System.IO;

namespace KTM
{
    class Template
    {
        private int id;
        private bool isError;
        private bool isOvershoot;
        private int hertz; //The frequency in cycles/second at which to resample.
        private float dist_crow; //"as the crow flies" distance of this template's path
        private List<TimePointF> raw_points; //all raw points that make up this template's path as read in by the log file
        private List<PointF> resampled_vel; //contains the temporally resampled VELOCITY points making up the ENTIRE path
        private List<TimePointF> filtered_points;

        private List<TimePointF> cur_raw_points; //the subset of raw points based on the value of cur_num_points
        private List<PointF> cur_resampled_vel; //the resampled velocity values based on cur_raw_points
        private List<PointF> cur_smoothed_vel; //the smoothed velocity values based on cur_resampled_vel
        private float cur_time; //the current timestamp of the most recent point once resampled. MUST be a multiple of 10 (assuming 100Hz resampling)
        private float cur_pct_time; //the percentage of the total path that has been completed based on time
        private float cur_pct_dist_crow; //the percentage of the total path that has been completed based on as-the-crow-flies distance

        private float velScoreDiff = 0f;
        private float cumVelScore = 0f;

        private List<List<PointF>> compareLst;


        /// <summary>
        /// The standard deviation of the Gaussian kernel to use. The greater the standard deviation, 
        /// the larger the kernel, and the smoother the result, as more neighboring values are taken into 
        /// account when computing the current value. The two-sided kernel will be of size
        /// 3 * <i>stdev</i> * 2 + 1, which means each smoothed value takes this many resampled values into
        /// account according to the weighting given by the kernel at each position.
        /// </summary>
        private int gaussianStdDev; // 3*(5)*2+1 = 31 kernel size.

        /// <summary>
        /// The weighting kernel to be used as a 1D convolution filter. The kernel size is based on
        /// the standard deviation of a Gaussian curve, which reaches zero at about 3 times this
        /// value.
        /// </summary>
        private readonly double[] Kernel;


        #region Properties

        public bool isOverShoot
        {
            get { return this.isOvershoot; }
        }

        public float VelScoreDiff
        {
            get { return this.velScoreDiff; }
            set { this.velScoreDiff = value; }
        }

        public float CumVelScore
        {
            get { return this.cumVelScore; }
            set { this.cumVelScore = value; }
        }


        public int Hertz
        {
            get { return this.hertz; }
        }

        public int GaussianStdDev
        {
            get { return this.gaussianStdDev; }
            set { }
        }

        public int ID
        {
            get { return this.id; }
        }

        public float DistCrow
        {
            get { return this.dist_crow; }
        }

        public List<TimePointF> RawPoints
        {
            get { return this.raw_points; }
        }

        public List<TimePointF> CurRawPoints
        {
            get { return this.cur_raw_points; }
        }

        public List<PointF> ResampledVel
        {
            get { return this.resampled_vel; }
        }

        public float PctTime
        {
            get { return this.cur_pct_time; }
        }

        public float PctDistCrow
        {
            get { return this.cur_pct_dist_crow; }
        }

        public float CurTime
        {
            get { return cur_time; }
        }

        public bool CompHasNext
        {
            get { return compareLst.Count > 0; }
        }

        #endregion

        /// <summary>
        /// Template constructor
        /// </summary>
        /// <param name="id">the templates id</param>
        /// <param name="isError">true if the movement used to create this template missed the target when it was collected. false otherwise</param>
        /// <param name="targCenter">the (x,y) coordinate of the center of the target that was used</param>
        /// <param name="points">the list of movement points to be used for creating this template</param>
        /// <param name="hz">the frequency to resample the points at</param>
        /// <param name="stdev">the standard deviation to use for the Gaussian kernel filter</param>
        /// <param name="is1D">true if the movement used for this template came from a 1D task. false otherwise</param>
        /// <param name="isOvershoot">true if the movement used for this template overshot the target</param>
        public Template(int id, bool isError, PointF targCenter, List<TimePointF> points, int hz, int stdev, bool is1D, bool isOvershoot)
        {
            this.id = id;

            this.hertz = hz;
            this.gaussianStdDev = stdev;
            Kernel = SeriesEx.GaussianKernel(GaussianStdDev);
            this.raw_points = new List<TimePointF>();
            this.filtered_points = new List<TimePointF>();
            this.raw_points.AddRange(points);

            //temporally resample the movement points
            List<TimePointF> temp = SeriesEx.ResampleInTime(points, hertz);
            
            //create velocity profile from the temporally resampled points
            this.resampled_vel = SeriesEx.Derivative(temp);

            dist_crow = CalcCrowDist(this.raw_points);

            compareLst = new List<List<PointF>>();
            this.isOvershoot = isOvershoot;
            this.isError = isError;
        }

        /// <summary>
        /// Filter the movement points for overshoots
        /// </summary>
        public void Filter()
        {
            List<TimePointF> points = new List<TimePointF>();
            points.Add(this.raw_points[0]);
            points.Add(this.raw_points[1]);

            int index = 2;
            while(index < this.raw_points.Count && GeotrigEx.Distance(this.raw_points[index], this.raw_points[0]) >= GeotrigEx.Distance(this.raw_points[index - 1], this.raw_points[0]))
            {
                points.Add(this.raw_points[index]);
                index++;
            }
            //add the rest of the points to the filtered list
            for(int i = index; i < this.raw_points.Count; i++)
            {
                this.filtered_points.Add(this.raw_points[i]);
            }

            if (this.filtered_points.Count > 0)
            {
                this.isOvershoot = true;
            }
        }

        public void NextComp()
        {
            if (CompHasNext)
            {
                this.cur_resampled_vel = compareLst[0];
                compareLst.RemoveAt(0);
            }

        }

        /// <summary>
        /// sets the value of cur_num_points and
        /// creates new lists using the appropriate
        /// number of raw points
        /// </summary>
        /// <param name="n">the number of raw points to use</param>
        public void SetNumPoints(int n)
        {
            //create a subset of n raw points
            this.cur_raw_points = new List<TimePointF>();
            for (int i = 0; i < n; i++)
                this.cur_raw_points.Add(raw_points[i]);

            if (this.cur_resampled_vel == null)
                this.cur_resampled_vel = new List<PointF>();

            //create resampled velocity points.
            List<TimePointF> temp = SeriesEx.ResampleInTime(this.cur_raw_points, Hertz);
            this.cur_resampled_vel = SeriesEx.Derivative(temp);
            compareLst.Add(this.cur_resampled_vel);
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

        /// <summary>
        /// Compare this template to another template
        /// </summary>
        /// <param name="other">the template to compare to</param>
        /// <returns>the total difference between the velocity points of the templates at each timestamp</returns>
        public float CompareTo(Template other)
        {
            this.cur_smoothed_vel = SeriesEx.Filter(this.cur_resampled_vel, Kernel);

            List<PointF> other_vel_sub = new List<PointF>();
            List<PointF> other_smooth_vel = new List<PointF>();

            //make sure that the other velocity profile is no longer
            //than this template's velocity profile
            for (int i = 0; i < this.cur_resampled_vel.Count; i++)
            {
                if (i < other.ResampledVel.Count)
                {
                    other_vel_sub.Add(other.ResampledVel[i]);
                }
            }

            //smooth the other templates velocity profile
            other_smooth_vel = SeriesEx.Filter(other_vel_sub, Kernel);

            this.cur_pct_time = (float)cur_resampled_vel.Last().X / (float)resampled_vel.Last().X;
            this.cur_pct_dist_crow = CalcCrowDist(this.cur_raw_points) / CalcCrowDist(this.raw_points);
            this.cur_time = this.cur_resampled_vel.Last().X;

            //compare the smoothed velocity profiles
            float score = 0f;
            for (int i = 0; i < this.cur_smoothed_vel.Count; i++)
            {
                if (i < other_smooth_vel.Count)
                    score += Math.Abs(cur_smoothed_vel[i].Y - other_smooth_vel[i].Y);
                else
                    score += cur_smoothed_vel[i].Y;

            }
            
            //normalize the score before returning
            score /= (float)cur_smoothed_vel.Count;
            return score;
        }

        /// <summary>
        /// Output the points in this template to to given streamwriter
        /// </summary>
        /// <param name="writer">the streamwriter to print the points to</param>
        public void PrintTemplate(StreamWriter writer)
        {
            foreach (TimePointF pt in this.raw_points)
                writer.WriteLine(this.id + "," + pt.X + "," + pt.Y + "," + pt.Time + "," + this.isError.ToString());
        }
    }
}
