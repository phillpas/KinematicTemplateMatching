===================================
MOUSE POINTING ENDPOINT PREDICTION 
USING KINEMATIC TEMPLATE MATCHING
===================================

Pasqual, P.T. and Wobbrock, J.O. (2014). Mouse pointing endpoint prediction using kinematic template matching. 
Proceedings of the ACM Conference on Human Factors in Computing Systems (CHI '14). Toronto, Ontario (April 26-May 1, 2014). New York: ACM Press, pp. 743-752.

The given application is divided into three sections:
"Collect"
"Analyze"
"Test"
all of which can be accessed through the "File" menu.

+++++++++
COLLECT
+++++++++
Collects and logs raw mouse movement data for both 1D and 2D tasks.
-Data is by default logged in "bin/Debug/Subject_{subject ID}" 
-A single "trial" is representative of one mousement ending with a click
-A "block" is a set of trials (mainly implemented as a way to give participants a break)
-A session can contain many blocks

+++++++++
ANALYZE
+++++++++
Analyzes the selected subjects and outputs analyses to a file ("bin/Debug/analysis.csv" by default)
-A script/statistics package can then be used to compute any results (predictive accuracy, hit rates, etc.)
-If the "Compare Library" box is checked, the subject selected from the dropdown menu will be used as the template libary to compare with.
-NOTE: While analysis is being performed, the UI will freeze. If you would like to see progress, check the console.

+++++++++
TEST
+++++++++
Basic demonstration of KTM's ability to predict in real time.
-A template library will be built from the selected subject's previously collected mouse movement data.
-Predictions are shown as small red dots on the screen.
