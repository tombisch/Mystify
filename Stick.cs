/*
 * File: Stick.cs
 * Author: Tom Bisch
 * Date: October 22, 2015
 * Description: This class represents a stick object to be drawn on the window 
 *              and contains methods to update the stick position and attributes.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;

namespace SETMystify {

    class Stick {

        // class members
        public const int LINE_SPACING = 10; // distance between each line
        public const int LINE_LIMIT = 20;   // max number of lines allowed
        public const int SPEED = 40;        // speed of stick

        // instance members
        private int index;                  // index of stick in list
        private int lineLength;             // length in pixels from line center to end
        private int lineAngle;              // angle of line           
        private Point currPoint;            // works with prevPoint to measure direction
        private Point prevPoint;            // works with currPoint to measure direction
        private List<Line> lines;           // all lines that compose a stick        
        private Form window;                // window to invalidate when stick is updated
        private bool linePrepared;          // indicates whether the next line has been calculated
        private bool lineLengthIncreasing;  // indicates whether the line length is increasing 

        // constructor
        public Stick(Form w) {
            Random rand = new Random();
            index = 0;
            lineLength = rand.Next(10, 50); // random value between 10 and 50
            lineAngle = rand.Next(0, 180); // random value between 0 - 180
            currPoint = new Point(0, 0);
            prevPoint = new Point(0, 0);
            lines = new List<Line>();       
            window = w;
            linePrepared = false;
            lineLengthIncreasing = false;
        }

        /* 
         * Function: UpdateList
         * Parameters: List<Stick> sticks - the list of sticks drawn to the form
         * Returns: None
         * Description: This method takes the lines that represent an instance of a stick
         *              and copies them into the stick list at the index provided to the instance
         *              updating the stick list to be drawn to the form.
         */
        private void UpdateList(List<Stick> sticks) {
            // prevent any other threads from accessing the list
            lock (frmWindow.syncSticks) {
                // copy lines from this instance to the stick in the draw list at position: index
                sticks[index].lines = lines;
                // refresh form
                window.Invalidate();
            }
            // reset linePrepared so thread can calculate next line
            linePrepared = false;
        }

        /* 
         * Function: UpdateStick
         * Parameters: object data (List<Stick> sticks) - the list of sticks drawn to the form
         * Returns: None
         * Description: This method/thread auto maneuvers the stick around the form.
         *              A timer is used to update the stick list on regular intervals,
         *              otherwise the method calculates the next line position for the 
         *              stick instance and checks for pause/user quit signals.
         */
        public void UpdateStick(object data) {
            bool quit = false;
            bool pause = false;
            int currToBorder = 0;
            int prevToCurr = 0;
            // timer used to automatically add a line at regular intervals
            System.Timers.Timer drawTimer = new System.Timers.Timer();
            drawTimer.Interval = SPEED;
            drawTimer.AutoReset = false; 
            drawTimer.Start();
            // call UpdateList each time the elapsed event is fired
            drawTimer.Elapsed += delegate { UpdateList((List<Stick>) data); };
            // continue until user has quit the program
            while (quit == false) {
                // linePrepared indicates if the next line has already been calculated
                if (linePrepared == false) {
                    // find the next point based on the current and previous points
                    Point nextPoint = FindPoint(currPoint, LINE_SPACING, FindAngle(prevPoint, currPoint));
                    // check whether the next point will go out of bounds
                    // if so, move previous point to change stick direction to point back into the form
                    // if not, simply move forward in a straight path
                    if (nextPoint.X < 0) {
                        // hit left border    
                        currToBorder = currPoint.X;
                        nextPoint = FindPoint(currPoint, currToBorder, FindAngle(prevPoint, currPoint));
                        currPoint = nextPoint;
                        prevToCurr = prevPoint.X - currPoint.X;
                        prevPoint = new Point(currPoint.X - prevToCurr, prevPoint.Y);
                    } else if (nextPoint.X > window.ClientSize.Width) {
                        // hit right border
                        currToBorder = window.ClientSize.Width - currPoint.X;
                        nextPoint = FindPoint(currPoint, currToBorder, FindAngle(prevPoint, currPoint));
                        currPoint = nextPoint;
                        prevToCurr = currPoint.X - prevPoint.X;
                        prevPoint = new Point(currPoint.X + prevToCurr, prevPoint.Y);
                    } else if (nextPoint.Y < 0) {
                        // hit top border
                        currToBorder = currPoint.Y;
                        nextPoint = FindPoint(currPoint, currToBorder, FindAngle(prevPoint, currPoint));
                        currPoint = nextPoint;
                        prevToCurr = prevPoint.Y - currPoint.Y;
                        prevPoint = new Point(prevPoint.X, currPoint.Y - prevToCurr);
                    } else if (nextPoint.Y > window.ClientSize.Height) {
                        // hit bottom border
                        currToBorder = window.ClientSize.Height - currPoint.Y;
                        nextPoint = FindPoint(currPoint, currToBorder, FindAngle(prevPoint, currPoint));
                        currPoint = nextPoint;
                        prevToCurr = currPoint.Y - prevPoint.Y;
                        prevPoint = new Point(prevPoint.X, currPoint.Y + prevToCurr);
                    } else {
                        // no border hit
                        prevPoint = currPoint;
                        currPoint = nextPoint;
                    }                 
                    // adjust size and angle of next line
                    UpdateLineLength();
                    UpdateLineAngle();
                    // calculate next draw line and add to stick
                    Point p1 = FindPoint(currPoint, lineLength, lineAngle);
                    Point p2 = FindPoint(currPoint, lineLength, lineAngle + 180);
                    Line drawLine = new Line(p1.X, p1.Y, p2.X, p2.Y);                    
                    lines.Add(drawLine);
                    // determine whether the stick has reached its lineLimit
                    if (lines.Count() > LINE_LIMIT) {
                        // remove the oldest line stored at front of list
                        lines.RemoveAt(0);
                    }
                    // the line is ready
                    // wait for elapsed event to fire to update the stick
                    linePrepared = true;
                }          
                // check for user to pause sticks
                pause = frmWindow.pauseSticks;       
                if (pause == true) {
                    // stop the timer
                    drawTimer.Stop();
                } else {
                    // start the timer
                    drawTimer.Start();
                }
                // check for to user quit
                quit = frmWindow.userQuit;
            }
            // release timer resources
            drawTimer.Dispose();
        }

        /* 
         * Function: FindPoint
         * Parameters: Point anchor - the origin of reference to the point to find
         *             int radius - the distance between the anchor and point to find
         *             int angle - the angle indicating where the point to find is
         * Returns: The point that is found on the imaginary circle
         * Description: This method finds a point using the parametric equation of a circle
         *              which requires an anchor point, a radius and an angle.
         */
        public Point FindPoint(Point anchor, int radius, int angle) {
            // use parametric equation of circle to find point
            int x = (int)(radius * Math.Cos(angle * Math.PI / 180)) + anchor.X;
            int y = (int)(radius * Math.Sin(angle * Math.PI / 180)) + anchor.Y;
            return new Point(x, y);
        }

        /* 
         * Function: FindAngle
         * Parameters: Point p1, Point p2
         * Returns: The angle between the 2 points
         * Description: This method finds the angle between 2 points based on the screens X and Y axis.
         */
        public int FindAngle(Point p1, Point p2) {
            // http://stackoverflow.com/questions/7586063/how-to-calculate-the-angle-between-a-line-and-the-horizontal-axis
            int deltaX = p2.X - p1.X;
            int deltaY = p2.Y - p1.Y;
            return (int)(Math.Atan2(deltaY, deltaX) * 180.0 / Math.PI);
        }

        /* 
         * Function: UpdateLineLength
         * Parameters: None
         * Returns: None
         * Description: This method simply increments or decrements the line length based on
         *              whether lineLengthIncreasing is true or false. If the line length minimum 
         *              or maximum is reached, lineLengthIncreasing is changed.
         */
        public void UpdateLineLength() {
            if (lineLengthIncreasing == true) {
                if (lineLength < 50) {
                    lineLength += 5;
                } else {
                    lineLengthIncreasing = false;
                }
            } else {
                if (lineLength > 10) {
                    lineLength -= 5;
                } else {
                    lineLengthIncreasing = true;
                }
            }
        }

        /* 
         * Function: UpdateLineAngle
         * Parameters: None
         * Returns: None
         * Description: This method increments the line angle and resets the angle
         *              to 0 when the limit is reached.
         */
        public void UpdateLineAngle() {
            if (lineAngle >= 180) {
                lineAngle = 0;
            } else {
                lineAngle += 5;
            }
        }

        // accessors and mutators
        public int Index {
            get { return index; }
            set { index = value; }
        }
        public int LineLength {
            get { return lineLength; }
            set { lineLength = value; }
        }
        public int LineAngle {
            get { return lineAngle; }
            set { lineAngle = value; }
        }
        public Point CurrPoint {
            get { return currPoint; }
            set { currPoint = value; }
        }
        public Point PrevPoint {
            get { return prevPoint; }
            set { prevPoint = value; }
        }
        public List<Line> Lines {
            get { return lines; }
            set { lines = value; }
        }     
        public Form Window {
            get { return window; }
            set { window = value; }
        }
        public bool LinePrepared {
            get { return linePrepared; }
            set { linePrepared = value; }
        }
        public bool LineLengthIncreasing {
            get { return lineLengthIncreasing; }
            set { lineLengthIncreasing = value; }
        }
    }
}
