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
using WobbrockLib;
using WobbrockLib.Extensions;
using System.IO;
using System.Media;

namespace KTM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static MainWindow mainWindow; //used to access this window from other windows/controls

        //ui elements
        CollectControl collectControl;
        AnalyzeControl analyzeControl;
        TestControl testControl;

        private Shape curTarget;
        private SolidColorBrush targBrush;
        private SolidColorBrush errorBrush;

        private const double MinDblClickDist = 4.0; // minimum distance two clicks must be apart (filters double-clicks)

        //options
        private int subjNum;
        private string gender;
        private const int MIN_A = 100; //the minimum amplitude of targets
        private const int MAX_A = 800; //the maximum amplitude of targets
        private int numTrials; //the number of trials per block
        private int numBlocks; //the number of blocks
        private bool is1D;
        private int blocksCompleted;
        private bool isCollecting;

        //for real time prediction (testing)
        private bool isTesting;
        private string selectedLib; //the selected template library to use for real time predictions
        private TemplateLibrary testLib;
        private RTTemplate curTemplate;
        private List<TimePointF> predictions;
        private List<string> rtOutput; //holds all of the prediction data to be outputted at the end of testing

        private string baseDir;
        private string curDir;
        private SessionData _sData;
        private BlockData _bData;
        private TrialData _tData;

        private List<TimePointF> preds = new List<TimePointF>();

        #region Properties

        public int SubjNum
        {
            get { return this.subjNum; }
            set { this.subjNum = value; }
        }

        public string Gender
        {
            get { return this.gender; }
            set { this.gender = value; }
        }

        public bool Is1D
        {
            get{return this.is1D;}
            set{this.is1D = value;}
        }

        public int NumTrials
        {
            get { return this.numTrials; }
            set { this.numTrials = value; }
        }

        public string SelectedLib
        {
            get { return this.selectedLib; }
            set { this.selectedLib = value; }
        }

        public int NumBlocks
        {
            get { return this.numBlocks; }
            set { this.numBlocks = value; }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(Main_KeyDown);

            mainWindow = this;
            isCollecting = false;
            isTesting = false;
            baseDir = Directory.GetCurrentDirectory();

            errorBrush = new SolidColorBrush();
            errorBrush.Color = Color.FromArgb(255, 255, 0, 0);

        }

        private void MenuItemCollect_Click(object sender, RoutedEventArgs e)
        {
            collectControl = new CollectControl();
            Canvas.SetTop(collectControl, Height/2 - 150);
            Canvas.SetLeft(collectControl, Width / 2 - 250);
            mainCanvas.Children.Add(collectControl);
        }

        private void MenuItemAnalyze_Click(object sender, RoutedEventArgs e)
        {
            analyzeControl = new AnalyzeControl();
            Canvas.SetTop(analyzeControl, Height / 2 - 150);
            Canvas.SetLeft(analyzeControl, Width / 2 - 250);
            mainCanvas.Children.Add(analyzeControl);
        }

        private void MenuItemTest_Click(object sender, RoutedEventArgs e)
        {
            testControl = new TestControl();
            Canvas.SetTop(testControl, Height / 2 - 150);
            Canvas.SetLeft(testControl, Width / 2 - 250);
            mainCanvas.Children.Add(testControl);
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCollecting)
            {
                
                TimePointF move = new TimePointF(e.GetPosition(mainCanvas).X, e.GetPosition(mainCanvas).Y, e.Timestamp);
                bool added = _tData.AddMove(move);

                if (isTesting && !_tData.IsStartAreaTrial && added)
                {
                    curTemplate.AddPoint(move);
                    TimePointF prediction;
                    if (is1D)
                        prediction = curTemplate.Predict1D(testLib);
                    else
                        prediction = curTemplate.Predict2D(testLib);
                    Ellipse ellipse = new Ellipse();
                    SolidColorBrush brush = new SolidColorBrush();
                    brush.Color = Color.FromArgb(255, 222, 4, 0);
                    ellipse.Fill = brush;
                    ellipse.Width = 3;
                    ellipse.Height = 3;
                    Canvas.SetTop(ellipse, prediction.Y);
                    Canvas.SetLeft(ellipse, prediction.X);
                    mainCanvas.Children.Add(ellipse);
                    preds.Add(prediction);
                }
            }
        }

        /// <summary>
        /// Called when the "Start" button is clicked in the CollectControl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectStart_Click(object sender, RoutedEventArgs e)
        {
            if (collectControl != null) { mainCanvas.Children.Remove(collectControl); }
            this.isCollecting = true;

            if (is1D)
                curTarget = new Rectangle();
            else
                curTarget = new Ellipse();

            targBrush = new SolidColorBrush();
            targBrush.Color = Color.FromArgb(255, 0, 204, 102);
            curTarget.Fill = targBrush;
            mainCanvas.Children.Add(curTarget);

            _sData = new SessionData(subjNum, gender, 20, MIN_A, MAX_A, 32, numTrials, numBlocks, is1D);
            _bData = _sData.Blocks[0];
            _tData = _bData[0];

            curDir = String.Format("{0}\\Subject_{1}\\", baseDir, subjNum);
            if (!Directory.Exists(curDir))
                Directory.CreateDirectory(curDir);

            DrawTarget(_bData[0].TargetBounds);

            blocksCompleted = 0;
            txt_blocksCompleted.Text = blocksCompleted + "/" + numBlocks;
            txt_blocksCompleted.Visibility = Visibility.Visible;
            lbl_blocksCompleted.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Called when the "start" button is clicked in the TestControl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TestStart_Click(object sender, RoutedEventArgs e)
        {
            this.isTesting = true;
            curTemplate = new RTTemplate();
            rtOutput = new List<string>();
            predictions = new List<TimePointF>();
            testLib = new TemplateLibrary(selectedLib, 40,7);
            rtOutput.Add("num_points,win_id,raw_x,raw_y,time,pred_x,pred_y,pred_dT,pred_dist,act_x,act_y,targ_x,targ_y");
            mainCanvas.Children.Remove(testControl);
            CollectStart_Click(sender, e);
        }

        /// <summary>
        /// Called when the mouse is clicked over the mainCanvas element
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_sData != null)
            {
                TimePointF pt = new TimePointF(e.GetPosition(mainCanvas).X, e.GetPosition(mainCanvas).Y, e.Timestamp);
                if(WobbrockLib.Extensions.GeotrigEx.Distance(_tData.Start,pt) > MinDblClickDist) 
                    NextTrial(pt);
            }
        }

        private void DrawTarget(System.Drawing.RectangleF bounds)
        {
            preds.Clear();
            curTarget.Width = bounds.Width;
            curTarget.Height = bounds.Height;
            Canvas.SetLeft(curTarget, bounds.X);
            Canvas.SetTop(curTarget, bounds.Y);
        }

        private void NextTrial(TimePointF click)
        {
            if (isTesting && !_tData.IsStartAreaTrial)
            {
                foreach (string pred in curTemplate.PredData)
                {
                    rtOutput.Add(pred + "," + click.X + "," + click.Y + "," + (_tData.TargetBounds.X + _tData.TargetBounds.Width / 2) + "," + (_tData.TargetBounds.Y + _tData.TargetBounds.Height / 2));// + "," + curTemplate.RawPoints[curTemplate.RawPoints.Count - 1].X); // + "," + (_tdata.TargetBounds.X + _tdata.TargetBounds.Width / 2));
                }
                predictions.Clear();
                curTemplate.Clear();
                //clear "spray can" dots from canvas
                var dots = mainCanvas.Children.OfType<Ellipse>().ToList();
                foreach (var d in dots)
                {
                    //dont clear the current target
                    if(d.Height < 32)
                        mainCanvas.Children.Remove(d);
                }
            }
            if (_tData.IsStartAreaTrial) //click was to begin the first actual trial in the current block
            {
                if (!_tData.TargetContains(click)) //click missed the start target
                    DoError(click);
                else
                {
                    _tData = _bData[1];
                    _tData.Start = click;
                    DrawTarget(_tData.TargetBounds);
                }
            }
            else if (_tData.Number < _bData.NumTrials) //go to the next trial in the block
            {
                if (!_tData.TargetContains(click))
                {
                    _tData.IsError = true;
                    SystemSounds.Beep.Play();
                }
                _tData = _bData[_tData.Number + 1];
                _tData.Start = click;
                DrawTarget(_tData.TargetBounds);
            }
            else //end the block and go to the next, or end the session if done
            {
                if (!_tData.TargetContains(click))
                {
                    _tData.IsError = true;
                    SystemSounds.Beep.Play();
                }

                MessageBoxResult blockComplete_result = MessageBox.Show("Block Completed", "Confirm", MessageBoxButton.OK, MessageBoxImage.None);
                if (blockComplete_result == MessageBoxResult.OK)
                {
                    if (_sData.CurBlockIndex + 1 == _sData.NumBlocks) //we have run all blocks
                    {
                        if (isTesting)
                        {
                            StreamWriter w = new StreamWriter(curDir + "rtOutput.csv");
                            foreach (string line in rtOutput)
                                w.WriteLine(line);
                            Console.WriteLine("Done Writing RTOUTPUT");
                            w.Close();
                            isTesting = false;
                        }

                        _sData.logAll(curDir);
                        MessageBoxResult allComplete_result = MessageBox.Show("All blocks are done. Session complete!", "Done", MessageBoxButton.OK, MessageBoxImage.None);
                        //clear the canvas
                        mainCanvas.Children.Clear();
                        txt_blocksCompleted.Visibility = Visibility.Hidden;
                        lbl_blocksCompleted.Visibility = Visibility.Hidden;
                        isCollecting = false;
                    }
                    else //start a new block
                    {
                        _sData.CurBlockIndex += 1;
                        _bData = _sData.Blocks[_sData.CurBlockIndex];
                        _tData = _bData[0]; //special start-area trial at index 0
                        DrawTarget(_tData.TargetBounds);
                    }
                    blocksCompleted++;
                    txt_blocksCompleted.Text = blocksCompleted + "/" + numBlocks;
                }
            }
        }

        private void DoError(TimePointF click)
        {
            System.Media.SystemSounds.Beep.Play();
            curTarget.Fill = errorBrush;
            curTarget.Fill = targBrush;
        }

        /// <summary>
        /// When 'Esc' is pressed (ASCII 27), the current block is reset and begun again.
        /// </summary>
        /// <param name="sender">The sender of this event.</param>
        /// <param name="e">The arguments for this event.</param>
        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && isCollecting)
            {
                Console.WriteLine("Esc Pressed");
                if (_bData != null)
                {
                    _bData.ClearTrialData();
                    _tData = _bData[0];
                    DrawTarget(_bData[0].TargetBounds);
                }
            }
        }

        

    }
}
