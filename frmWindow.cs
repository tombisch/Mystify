/*
 * File: frmWindow.cs
 * Author: Tom Bisch
 * Date: October 22, 2015
 * Description: This class represents the form/window that provides the interaction
 *              between the user and the program. The class fires off events based on
 *              actions performed by the user via the mouse. When the user holds down the 
 *              left mouse button, a stick is created and follows the mouse. Holding down
 *              the right mouse button pauses all the sticks movement.
 */

using System;
using System.Collections.Generic; 
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SETMystify {
    public partial class frmWindow : Form {

        public static readonly object syncSticks = new object(); // used to sync list of sticks drawn to window
        public volatile static bool userQuit = false;            // indicates when the user has pressed the exit button
        public volatile static bool pauseSticks = false;         // indicates to the stick threads to pause
        private bool holdingMouseLeft = false;                   // indicates whether the left mouse button is pressed
        private bool holdingMouseRight = false;                  // indicates whether the right mouse button is pressed
        private Pen pen = new Pen(Color.Black, 2);               // pen used for drawing sticks
        private List<Stick> sticks = new List<Stick>();          // list used to store and draw all sticks
        private List<Thread> threads = new List<Thread>();       // list used to keep track of threads
        private Stick tmpStick;                                  // stick controlled by the user via mouse
        
        // constructor
        public frmWindow() {
            InitializeComponent();  
            tmpStick = new Stick(this);
        }

        // paint event handler
        private void frmWindow_Paint(object sender, PaintEventArgs e) {
            Graphics g = e.Graphics;
            // draw all auto sticks
            lock (syncSticks) {
                foreach (Stick s in sticks.ToList()) {
                    foreach (Line l in s.Lines.ToList()) {
                        g.DrawLine(pen, l.X1, l.Y1, l.X2, l.Y2);
                    }
                } 
            } 
            // draw tmp stick   
            foreach (Line l in tmpStick.Lines.ToList()) {
                g.DrawLine(pen, l.X1, l.Y1, l.X2, l.Y2);
            }
        }

        // mouse move event handler
        private void frmWindow_MouseMove(object sender, MouseEventArgs e) {
            // left mouse button pressed
            if (e.Button == MouseButtons.Left) {
                // on initial left mouse click
                if (holdingMouseLeft == false) {
                    holdingMouseLeft = true;
                    // set current point to equal previous point
                    tmpStick.CurrPoint = new Point(e.X, e.Y);
                    tmpStick.PrevPoint = new Point(tmpStick.CurrPoint.X, tmpStick.CurrPoint.Y);
                }                
                // measure distance between mouse position and current point
                Line meausureLine = new Line(e.X, e.Y, tmpStick.CurrPoint.X, tmpStick.CurrPoint.Y);          
                // add the next line to the stick
                if (meausureLine.Length() > Stick.LINE_SPACING) {
                    // set previous and current point
                    tmpStick.PrevPoint = new Point(tmpStick.CurrPoint.X, tmpStick.CurrPoint.Y);
                    tmpStick.CurrPoint = new Point(e.X, e.Y);            
                    // adjust size and angle of next line
                    tmpStick.UpdateLineLength();
                    tmpStick.UpdateLineAngle();
                    // calculate and add next line to draw to the stick
                    Point p1 = tmpStick.FindPoint(tmpStick.CurrPoint, tmpStick.LineLength, tmpStick.LineAngle);
                    Point p2 = tmpStick.FindPoint(tmpStick.CurrPoint, tmpStick.LineLength, tmpStick.LineAngle + 180);
                    Line drawLine = new Line(p1.X, p1.Y, p2.X, p2.Y);
                    tmpStick.Lines.Add(drawLine);                   
                }
                // check the lineLimit for the stick
                if (tmpStick.Lines.Count() > Stick.LINE_LIMIT) {
                    // remove the oldest line stored at front of list
                    tmpStick.Lines.RemoveAt(0);
                } 
            // right mouse button pressed
            } else if (e.Button == MouseButtons.Right) {
                // pause all sticks on initial right mouse click
                if (holdingMouseRight == false) {
                    holdingMouseRight = true;
                    pauseSticks = true;
                }     
            // no mouse button pressed
            } else {
                if (holdingMouseLeft == true) {
                    // send the stick to a thread to continue on own
                    StickDrawThread();
                    // reset the tmp stick
                    tmpStick.CurrPoint = new Point(0, 0);
                    tmpStick.PrevPoint = new Point(0, 0);
                    tmpStick.Lines = new List<Line>();
                }
                if (holdingMouseRight == true) {
                    // resume all sticks
                    pauseSticks = false;
                }
                holdingMouseLeft = false;
                holdingMouseRight = false;
            }
            // refresh form
            this.Invalidate();
        }
        
        // mouse down event handler
        private void frmWindow_MouseDown(object sender, MouseEventArgs e) {             
            // right mouse button pressed
            if (e.Button == MouseButtons.Right) {
                // pause all sticks on initial right mouse click
                if (holdingMouseRight == false) {
                    holdingMouseRight = true;
                    pauseSticks = true; // signals the threads to stop timers
                }     
            }
            // refresh form
            this.Invalidate();
        }

        /* 
         * Function: StickDrawThread
         * Parameters: None
         * Returns: None
         * Description: This method creates a new stick based on the current attributes 
         *              of tmpStick, adds the new stick to the stick draw list and then starts
         *              a new thread passing the stick draw list as a reference.
         */
        private void StickDrawThread() {     
            // create new stick and copy over required tmpStick attributes
            Stick s = new Stick(this);
            s.LineLength = tmpStick.LineLength;
            s.LineAngle = tmpStick.LineAngle;
            s.CurrPoint = tmpStick.CurrPoint;
            s.PrevPoint = tmpStick.PrevPoint;
            s.Lines = tmpStick.Lines;
            // prevent any current threads from accessing the list
            lock (syncSticks) {
                s.Index = sticks.Count;
                sticks.Add(s);
            }
            // create new thread, start thread, then add thread to thread list
            Thread t = new Thread(new ParameterizedThreadStart(s.UpdateStick));
            threads.Add(t);
            t.Start(sticks);    
        }

        // form closing event handler
        private void frmWindow_FormClosing(object sender, FormClosingEventArgs e) {
            userQuit = true; // signals the threads to exit
            foreach(Thread t in threads){
                t.Join();
            }
        }
    }
}
