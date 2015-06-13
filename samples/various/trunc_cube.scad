/*
    test case : truncated cube
    from "Two Color Archimedean Solids - "
    http://www.thingiverse.com/thing:19168
*/

union()
{
    // create the black pieces first so they
    // are embedded in cube
    color("black")
    faces3();

    color ("red")
    scale(2.49)
    trunc_cube();
}


module trunc_cube()
{

  /*
      truncated cube
     coordinates from Wikipedia
     triangles by pmoews
  */
polyhedron
       (points = [
                 [   4.14214,  10.00000,  10.00000],
                 [   4.14214, -10.00000,  10.00000],
                 [  -4.14214,  10.00000,  10.00000],
                 [  -4.14214, -10.00000,  10.00000],
                 [  10.00000,   4.14214,  10.00000],
                 [  10.00000,  -4.14214,  10.00000],
                 [ -10.00000,   4.14214,  10.00000],
                 [ -10.00000,  -4.14214,  10.00000],
                 [  10.00000,  10.00000,   4.14214],
                 [  10.00000, -10.00000,   4.14214],
                 [ -10.00000,  10.00000,   4.14214],
                 [ -10.00000, -10.00000,   4.14214],
                 [  10.00000,  10.00000,  -4.14214],
                 [  10.00000, -10.00000,  -4.14214],
                 [ -10.00000,  10.00000,  -4.14214],
                 [ -10.00000, -10.00000,  -4.14214],
                 [   4.14214,  10.00000, -10.00000],
                 [   4.14214, -10.00000, -10.00000],
                 [  -4.14214,  10.00000, -10.00000],
                 [  -4.14214, -10.00000, -10.00000],
                 [  10.00000,   4.14214, -10.00000],
                 [  10.00000,  -4.14214, -10.00000],
                 [ -10.00000,   4.14214, -10.00000],
                 [ -10.00000,  -4.14214, -10.00000],
                ],
           faces = [
                 [    0,    6,    2],[    4,    6,    0],
                 [    5,    6,    4],[    1,    6,    5],
                 [    3,    6,    1],[    7,    6,    3],
                 

                 [   22,   16,   18],[   20,   22,   21],
                 [   16,   22,   20],[   22,   17,   21],
                 [   22,   19,   17],[   22,   23,   19],
                 
                 [    8,    5,    4],[   12,    5,    8],
                 [   20,    5,   12],[   21,    5,   20],
                 [   13,    5,   21],[   13,    9,    5],
                 
                 [    7,   10,    6],[    7,   14,   10],
                 [    7,   22,   14],[    7,   23,   22],
                 [    7,   15,   23],[   11,   15,    7],
                 
                 [    2,    8,    0],[    2,   12,    8],
                 [    2,   16,   12],[    2,   18,   16],
                 [    2,   14,   18],[   10,   14,    2],
                 
                 [    9,    3,    1],[   13,    3,    9],
                 [   17,    3,   13],[   19,    3,   17],
                 [   15,    3,   19],[   15,   11,    3],
                 
                 [    4,    0,    8],[    2,    6,   10],
                 [    7,    3,   11],[    1,    5,    9],
                 [   16,   20,   12],[   21,   17,   13],
                 
                 [   22,   18,   14],[   19,   23,   15],

]
      );

}


  module faces3()
{




/*
   truncated cube
     coordinates from Wolfram/alpha
     triangles by pmoews  triangular faces
*/
polyhedron
       (points = [
                 [  10.35534,  25.00000,  25.00000],
                 [  10.35534,  25.00000, -25.00000],
                 [  10.35534, -25.00000,  25.00000],
                 [  10.35534, -25.00000, -25.00000],
                 [ -10.35534,  25.00000,  25.00000],
                 [ -10.35534,  25.00000, -25.00000],
                 [ -10.35534, -25.00000,  25.00000],
                 [ -10.35534, -25.00000, -25.00000],
                 [  25.00000,  10.35534,  25.00000],
                 [  25.00000,  10.35534, -25.00000],
                 [  25.00000, -10.35534,  25.00000],
                 [  25.00000, -10.35534, -25.00000],
                 [ -25.00000,  10.35534,  25.00000],
                 [ -25.00000,  10.35534, -25.00000],
                 [ -25.00000, -10.35534,  25.00000],
                 [ -25.00000, -10.35534, -25.00000],
                 [  25.00000,  25.00000,  10.35534],
                 [  25.00000,  25.00000, -10.35534],
                 [  25.00000, -25.00000,  10.35534],
                 [  25.00000, -25.00000, -10.35534],
                 [ -25.00000,  25.00000,  10.35534],
                 [ -25.00000,  25.00000, -10.35534],
                 [ -25.00000, -25.00000,  10.35534],
                 [ -25.00000, -25.00000, -10.35534],
                 [   9.31980,  22.50000,  22.50000],
                 [   9.31980,  22.50000, -22.50000],
                 [   9.31980, -22.50000,  22.50000],
                 [   9.31980, -22.50000, -22.50000],
                 [  -9.31980,  22.50000,  22.50000],
                 [  -9.31980,  22.50000, -22.50000],
                 [  -9.31980, -22.50000,  22.50000],
                 [  -9.31980, -22.50000, -22.50000],
                 [  22.50000,   9.31980,  22.50000],
                 [  22.50000,   9.31980, -22.50000],
                 [  22.50000,  -9.31980,  22.50000],
                 [  22.50000,  -9.31980, -22.50000],
                 [ -22.50000,   9.31980,  22.50000],
                 [ -22.50000,   9.31980, -22.50000],
                 [ -22.50000,  -9.31980,  22.50000],
                 [ -22.50000,  -9.31980, -22.50000],
                 [  22.50000,  22.50000,   9.31980],
                 [  22.50000,  22.50000,  -9.31980],
                 [  22.50000, -22.50000,   9.31980],
                 [  22.50000, -22.50000,  -9.31980],
                 [ -22.50000,  22.50000,   9.31980],
                 [ -22.50000,  22.50000,  -9.31980],
                 [ -22.50000, -22.50000,   9.31980],
                 [ -22.50000, -22.50000,  -9.31980],
                ],
           faces = [
                 [   27,   35,   43],[   11,    3,   19],
                 [   27,    3,   11],[   11,   35,   27],
                 [   35,   11,   19],[   19,   43,   35],
                 [   43,   19,    3],[   27,   43,    3],
                 
                 [   25,   41,   33],[   17,    1,    9],
                 [   25,    1,   17],[   17,   41,   25],
                 [   41,   17,    9],[    9,   33,   41],
                 [   33,    9,    1],[   25,   33,    1],
                 
                 [   28,   44,   36],[   20,    4,   12],
                 [   28,    4,   20],[   20,   44,   28],
                 [   44,   20,   12],[   12,   36,   44],
                 [   36,   12,    4],[   28,   36,    4],
                 
                 [   30,   38,   46],[   14,    6,   22],
                 [   30,    6,   14],[   14,   38,   30],
                 [   38,   14,   22],[   22,   46,   38],
                 [   46,   22,    6],[   30,   46,    6],
                 
                 [   31,   47,   39],[   23,    7,   15],
                 [   31,    7,   23],[   23,   47,   31],
                 [   47,   23,   15],[   15,   39,   47],
                 [   39,   15,    7],[   31,   39,    7],
                 
                 [   29,   37,   45],[   13,    5,   21],
                 [   29,    5,   13],[   13,   37,   29],
                 [   37,   13,   21],[   21,   45,   37],
                 [   45,   21,    5],[   29,   45,    5],
                 
                 [   26,   42,   34],[   18,    2,   10],
                 [   26,    2,   18],[   18,   42,   26],
                 [   42,   18,   10],[   10,   34,   42],
                 [   34,   10,    2],[   26,   34,    2],
        

                 
                 [   32,   40,   24],[   8,    0,    16],
                 [   16,    0,   24],[   24,   40,   16],
                 [   8,   16,    40],[    40,   32,   8],
                 [   0,    8,    32],[   0,   32,    24],
            ]
      );

}

