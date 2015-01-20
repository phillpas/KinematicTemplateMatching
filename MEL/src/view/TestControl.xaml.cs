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

namespace KTM
{
    /// <summary>
    /// Interaction logic for TestControl.xaml
    /// </summary>
    public partial class TestControl : UserControl
    {
        MainWindow mainWin;

        private int subjNum;
        private string gender;
        private int numTrials;
        private int numBlocks;
        private bool is1D;

        public TestControl()
        {
            InitializeComponent();
            mainWin = MainWindow.mainWindow;
            List<string> availableSubj = getAvailableSubjects();
            foreach (string subj in availableSubj)
            {
                lst_libraries.Items.Add(subj);
            }
            if (lst_libraries.Items.Count > 0)
            {
                lst_libraries.SelectedIndex = 0;
            }
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

        #region Event Handlers

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            mainWin.SubjNum = this.subjNum;
            mainWin.Gender = this.gender;
            mainWin.NumTrials = this.numTrials;
            mainWin.NumBlocks = this.numBlocks;
            mainWin.Is1D = this.is1D;
            mainWin.SelectedLib = lst_libraries.SelectedItem.ToString() + "\\Log" + (is1D ? "_1D.csv" : "_2D.csv");
            mainWin.TestStart_Click(sender, e);
        }

        private void SubjID_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            int newVal;
            bool isNum = Int32.TryParse(box.Text, out newVal);
            if (isNum)
                this.subjNum = newVal;
            else
            {
                this.subjNum = 0;
                box.Text = "0";
            }
        }

        private void Gender_RadioChecked(object sender, RoutedEventArgs e)
        {
            RadioButton checkedRadio = sender as RadioButton;
            this.gender = checkedRadio.Content.ToString();
        }

        private void Type_RadioChecked(object sender, RoutedEventArgs e)
        {
            RadioButton checkedRadio = sender as RadioButton;
            this.is1D = checkedRadio.Content.ToString().Contains("1D");
        }

        private void NumTrials_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            int newVal;
            bool isNum = Int32.TryParse(box.Text, out newVal);
            if (isNum)
                this.numTrials = newVal;
            else
            {
                this.numTrials = 0;
                box.Text = "0";
            }
        }

        private void NumBlocks_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            int newVal;
            bool isNum = Int32.TryParse(box.Text, out newVal);
            if (isNum)
                this.numBlocks = newVal;
            else
            {
                this.numBlocks = 0;
                box.Text = "0";
            }
        }

        #endregion

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            mainWin.mainCanvas.Children.Remove(this);
        }
    }
}
