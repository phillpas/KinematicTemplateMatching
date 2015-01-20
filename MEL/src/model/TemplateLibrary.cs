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
using System.IO;
using WobbrockLib;
using WobbrockLib.Extensions;
using System.Drawing;

namespace KTM
{
    class TemplateLibrary
    {

        struct tempPair
        {
            public float score;
            public int index;
        };

        private List<Template> library; //holds all of the templates in the library
        public int Hz;
        public int Stdev;
        private bool is1D;

        private StreamWriter tempWriter;

        /// <summary>
        /// Template Library constructor
        /// </summary>
        /// <param name="file">the path to the file to create the template library from</param>
        /// <param name="hz">the frequency at which to resample the points</param>
        /// <param name="stdev">the standard deviation to use for the Gaussian kernel filter</param>
        public TemplateLibrary(string file, int hz, int stdev)
        {
            library = new List<Template>();
            this.Hz = hz;
            this.Stdev = stdev;
            Load(file, false);
        }

        #region Properties

        public List<Template> Library
        {
            get { return this.library; }
        }

        public int NumTemplates
        {
            get { return this.library.Count; }
        }

        public Template this[int index]
        {
            get
            {
                if (0 <= index && index < this.library.Count)
                    return this.library[index];
                return null;
            }
        }

        #endregion

        /// <summary>
        /// Load a log file containing raw mouse points and
        /// turn them into a list of templates
        /// </summary>
        /// <param name="file">the csv log file to load</param>
        private void Load(string file, bool includeErrors)
        {
            StreamReader reader = new StreamReader(file);
            string line = reader.ReadLine(); //read the header
            is1D = file.Contains("1D");
            Dictionary<int, List<string>> paths = new Dictionary<int, List<string>>();

            string[] part = file.Split('.');
            tempWriter = new StreamWriter(part[0] + "_KEP.csv", false, Encoding.Default);

            int curID = 0;
            while ((line = reader.ReadLine()) != null)
            {
                //create templates from the file
                string[] parts = line.Split(',');
                curID = System.Convert.ToInt32(parts[0]);

                if (!paths.ContainsKey(curID))
                {
                    paths[curID] = new List<string>();
                }
                paths[curID].Add(line);
            }
            foreach (int id in paths.Keys)
            {
                List<string> data = paths[id];
                bool isError = data[0].Split(',')[4].Equals("True");
                int targx = 0;//System.Convert.ToInt32(data[0].Split(',')[5]);
                int targy = 0;// System.Convert.ToInt32(data[0].Split(',')[6]);
                PointF targCenter = new PointF(targx, targy);

                List<TimePointF> points = new List<TimePointF>();
                foreach (string ln in data)
                {
                    string[] parts = ln.Split(',');
                    int xpos = System.Convert.ToInt32(parts[1]);
                    int ypos = System.Convert.ToInt32(parts[2]);
                    long time = System.Convert.ToInt64(parts[3]);
                    TimePointF point = new TimePointF(xpos, ypos, time);
                    points.Add(point);
                }
                Template temp2 = new Template(id, isError, targCenter, points, Hz, Stdev, is1D, false);
                temp2.Filter();
                library.Add(temp2);
                points.Clear();
            }
            Console.Write("done");
            Console.WriteLine("Library Size: " + library.Count);
        }


        /// <summary>
        /// Load a log file containing raw mouse points and
        /// turn them into a list of templates. Ensure that the
        /// resulting template library contains excatly "size" templates 
        /// </summary>
        /// <param name="file">the csv log file to load</param>
        public void Load(string file, int size, bool includeErrors)
        {
            this.Load(file, includeErrors);
            if (size < library.Count)
            {
                Random rand = new Random();
                while (library.Count != size)
                {
                    int rIdx = rand.Next(library.Count);
                    library.RemoveAt(rIdx);
                }
            }
        }

        public List<string> Evaluate()
        {
            List<string> results = new List<string>();
            Random rand = new Random();

            //run on 10% of the library
            for (int j = 0; j < library.Count / 10; j++)
            {
                Console.WriteLine("J: " + j);
                //select a candidate template at random and remove it
                //from the template library
                int cIdx = rand.Next(library.Count);
                Template candidate = library[cIdx];
                while (candidate.isOverShoot)
                {
                    cIdx = rand.Next(library.Count);
                    candidate = library[cIdx];
                }
                candidate.PrintTemplate(tempWriter);
                library.RemoveAt(cIdx);

                for (int k = 4; k <= candidate.RawPoints.Count; k++)
                {
                    candidate.SetNumPoints(k);
                    List<tempPair> vel_pairs = new List<tempPair>();

                    Template win_temp = library[1];
                    int win_id = 1;
                    float win_score = float.MaxValue;
                    for (int tempIndex = 0; tempIndex < library.Count; tempIndex++)
                    {
                        Template temp = library[tempIndex];
                        float score_vel = candidate.CompareTo(temp);
                        float lastCum = temp.CumVelScore;
                        temp.CumVelScore += score_vel;

                        tempPair tp_vel;
                        tp_vel.index = tempIndex;
                        tp_vel.score = temp.CumVelScore;
                        vel_pairs.Add(tp_vel);

                        if (library[tempIndex].CumVelScore < win_score)
                        {
                            win_score = library[tempIndex].CumVelScore;
                            win_temp = library[tempIndex];
                            win_id = tempIndex;
                        }
                    }

                    string line = k + ",";


                    //calculate values for anaysis file
                    int candidate_id = candidate.ID;
                    int sigma_val = Stdev;
                    int num_points = k;
                    float pct_time = candidate.PctTime;
                    float pct_dist_crow = candidate.PctDistCrow;
                    float time = candidate.CurTime;

                    float win_crow_1D_error_unsigned = Math.Abs(candidate.DistCrow - win_temp.DistCrow);
                    float win_crow_1D_error_signed = candidate.DistCrow - win_temp.DistCrow;

                    //calc 2D error
                    float predDist = win_temp.DistCrow;
                    TimePointF actualEnd = candidate.RawPoints.Last();
                    TimePointF predEnd;
                    if (is1D)
                    {
                        if (candidate.CurRawPoints.Last().X < candidate.CurRawPoints[0].X)
                            predDist *= -1;
                        predEnd = new TimePointF(candidate.RawPoints[0].X + predDist, candidate.RawPoints[0].Y, candidate.RawPoints.Last().Time);
                    }
                    else
                    {
                        double radians = GeotrigEx.Angle(candidate.RawPoints[0], candidate.CurRawPoints.Last(), false);
                        float predx = candidate.RawPoints[0].X + (float)(Math.Cos(radians) * win_temp.DistCrow);
                        float predy = candidate.RawPoints[0].Y + (float)(Math.Sin(radians) * win_temp.DistCrow);
                        predEnd = new TimePointF(predx, predy, candidate.RawPoints.Last().Time);
                    }
                    float win_2D_error = (float)GeotrigEx.Distance(actualEnd, predEnd);

                    bool inTarget;
                    if (is1D)
                        inTarget = win_crow_1D_error_unsigned <= 16 ? true : false;
                    else
                        inTarget = win_2D_error <= 16 ? true : false;

                    //log data to analysis file
                    results.Add(candidate_id + "," + sigma_val + "," + Hz + "," + num_points + "," + pct_time + "," + pct_dist_crow + "," + "0" + "," + time + "," + win_id + "," + win_crow_1D_error_unsigned + "," + win_crow_1D_error_signed + "," + "0" + "," + "0" + "," + win_2D_error + "," + inTarget.ToString());
                }

                library.Insert(cIdx, candidate);
                foreach (Template temp in library)
                {
                    temp.CumVelScore = 0f;
                }
            }
            tempWriter.Close();
            return results;
        }

        public List<string> Evaluate(TemplateLibrary other)
        {
            List<string> results = new List<string>();

            for (int j = 0; j < other.library.Count; j++)
            {
                Console.WriteLine("J: " + j);
                //select a candidate template at random and remove it
                //from the template library
                Template candidate = other.library[j];

                for (int k = 1; k <= candidate.RawPoints.Count; k++)
                {
                    candidate.SetNumPoints(k);

                    while (candidate.CompHasNext)
                    {
                        candidate.NextComp();
                        Template win_temp = library[0];
                        int win_id = 0;
                        float win_score = float.MaxValue;
                        for (int tempIndex = 0; tempIndex < library.Count; tempIndex++)
                        {
                            float score = candidate.CompareTo(library[tempIndex]);

                            if (score < win_score)
                            {
                                win_score = score;
                                win_temp = library[tempIndex];
                                win_id = tempIndex;
                            }
                        }

                        //calculate values for anaysis file
                        int candidate_id = candidate.ID;
                        int sigma_val = Stdev;
                        int num_points = k;
                        float pct_time = candidate.PctTime;
                        float pct_dist_crow = candidate.PctDistCrow;
                        float time = candidate.CurTime;

                        float win_crow_1D_error_unsigned = Math.Abs(candidate.DistCrow - win_temp.DistCrow);
                        float win_crow_1D_error_signed = candidate.DistCrow - win_temp.DistCrow;

                        //calc 2D error
                        float predDist = win_temp.DistCrow;
                        TimePointF actualEnd = candidate.RawPoints.Last();
                        TimePointF predEnd;
                        if (is1D)
                        {
                            if (candidate.CurRawPoints.Last().X < candidate.CurRawPoints[0].X)
                                predDist *= -1;
                            predEnd = new TimePointF(candidate.RawPoints[0].X + predDist, candidate.RawPoints[0].Y, candidate.RawPoints.Last().Time);
                        }
                        else
                        {
                            double radians = GeotrigEx.Angle(candidate.RawPoints[0], candidate.CurRawPoints.Last(), false);
                            float predx = candidate.RawPoints[0].X + (float)(Math.Cos(radians) * win_temp.DistCrow);
                            float predy = candidate.RawPoints[0].Y + (float)(Math.Sin(radians) * win_temp.DistCrow);
                            predEnd = new TimePointF(predx, predy, candidate.RawPoints.Last().Time);
                        }
                        float win_2D_error = (float)GeotrigEx.Distance(actualEnd, predEnd);
                        results.Add(candidate_id + "," + sigma_val + "," + Hz + "," + num_points + "," + pct_time + "," + pct_dist_crow + "," + "0" + "," + time + "," + win_id + "," + win_crow_1D_error_unsigned + "," + win_crow_1D_error_signed + "," + "0" + "," + "0" + "," + win_2D_error);
                    }
                }
            }
            return results;
        }

    }
}
