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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using WobbrockLib;

namespace KTM
{
    /// <summary>
    /// Interaction logic for AnalyzeControl.xaml
    /// </summary>
    public partial class AnalyzeControl : UserControl
    {
        private StreamWriter aOutput; //the analysis output file
        MainWindow mainWin;

        public AnalyzeControl()
        {
            InitializeComponent();
            List<string> availableSubj = getAvailableSubjects();
            foreach (string subj in availableSubj)
            {
                lst_available.Items.Add(subj);
                combobox_compare.Items.Add(subj);
            }
            mainWin = MainWindow.mainWindow;
        }

        /// <summary>
        /// Scans the "bin" directory and finds all available subjects
        /// that have had data logged
        /// </summary>
        /// <returns>a list of strings containing all available subjects</returns>
        private List<string> getAvailableSubjects()
        {
            List<string> dirs = new List<string>();
            DirectoryInfo dir = new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
            foreach (System.IO.DirectoryInfo g in dir.GetDirectories())
            {
                string[] parts = g.Name.Split('_');
                if (parts[0].Equals("Subject"))
                {
                    dirs.Add(g.Name);
                }
            }
            return dirs;
        }

        /// <summary>
        /// Adds all of the subjects to the list of subjects
        /// that will be analyzed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_addAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < lst_available.Items.Count; i++)
            {
                lst_selected.Items.Add(lst_available.Items[i]);
                lst_available.Items.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// Adds the selected subject to the list of subjects 
        /// that will be analyzed 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            string subj = lst_available.SelectedItem.ToString();
            if (!lst_selected.Items.Contains(subj))
            {
                lst_selected.Items.Add(subj);
                lst_available.Items.Remove(subj);
            }
        }

        /// <summary>
        /// Removes the selected subject from the list of subjects
        /// that will be analyzed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            string subj = lst_selected.SelectedItem.ToString();
            if (!lst_available.Items.Contains(subj))
            {
                lst_available.Items.Add(subj);
                lst_selected.Items.Remove(subj);
            }
        }

        /// <summary>
        /// Removes all subjects from the list of subjects
        /// that will be analyzed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_removeAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < lst_selected.Items.Count; i++)
            {
                lst_available.Items.Add(lst_selected.Items[i]);
                lst_selected.Items.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// Performs analysis on all selected subjects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_analyze_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();

            aOutput = new StreamWriter("analysis.csv", false);
            //write header
            aOutput.WriteLine("subject_id,dimension,candidate_id,sigma_val,Hz,num_points,pct_time,pct_dist_crow,pct_dist_path,time,win_id,win_crow_1D_error_unsigned,win_crow_1D_error_signed,win_trail_1D_error_unsigned,win_trail_1D_error_signed,win_2D_error,inTarget?");

            for (int i = 0; i < lst_selected.Items.Count; i++)
            {
                string dir = (string)lst_selected.Items[i];
                int subjNum = System.Convert.ToInt32(lst_selected.Items[i].ToString().Split('_')[1]);

                TemplateLibrary templateLib;
                
                //use another subject's templates as the template library
                if ((bool)checkbox_compare.IsChecked && combobox_compare.SelectedItem != null)
                {
                    templateLib = new TemplateLibrary(combobox_compare.SelectedItem.ToString() + "\\Log.csv", 20, 7);
                    TemplateLibrary testLib = new TemplateLibrary(dir + "\\Log.csv", 20, 7);
                    List<string> results = templateLib.Evaluate(testLib);
                    foreach (string r in results)
                        aOutput.WriteLine(subjNum + "," + r);
                }
                else
                {
                    int STDEV = 7 ;
                    int HZ = 20 ;

                    //for 1D
                    Console.WriteLine("1D::Now Evaluating:::Stdev:" + STDEV + ",Hz:" + HZ);
                    templateLib = new TemplateLibrary(dir + "\\Log_1D.csv", HZ, STDEV);
                    List<string> results = templateLib.Evaluate();
                    foreach (string r in results)
                        aOutput.WriteLine(subjNum + ",1D," + r);

                    //for 2D
                    Console.WriteLine("2D::Now Evaluating:::Stdev:" + STDEV + ",Hz:" + HZ);
                    templateLib = new TemplateLibrary(dir + "\\Log_2D.csv", HZ, STDEV);
                    results = templateLib.Evaluate();
                    foreach (string r in results)
                        aOutput.WriteLine(subjNum + ",2D," + r);
                }

            }
            aOutput.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            mainWin.mainCanvas.Children.Remove(this);
        }
    }
}
