/* from FreeCAD:importCSG.py */

%token group
%token sphere
%token cylinder
%token cube
%token multmatrix
%token intersection
%token difference
%token union
%token rotate_extrude
%token linear_extrude
%token true
%token false
%token circle
%token square
%token polygon
%token paths
%token points
%token undef
%token polyhedron
%token triangles
%token faces
%token render
%token surface
%token subdiv
%token glide
%token hull
%token minkowski
%token projection
%token import
%token color
%token offset
%token resize

%token WORD
%token NUMBER
%token LPAREN
%token RPAREN
%token OBRACE
%token EBRACE
%token OSQUARE
%token ESQUARE
%token COMMA
%token SEMICOL
%token EQ
%token STRING
%token ID
%token DOT
%token MODIFIERBACK
%token MODIFIERDEBUG
%token MODIFIERROOT
%token MODIFIERDISABLE

%%

block_list : statement
           | block_list statement
           | statementwithmod
           | block_list statementwithmod
           ;

statement : part
           | operation
           | multmatrix_action
           | group_action1
           | group_action2
           | color_action
           | render_action
           | not_supported
           ;

statementwithmod : anymodifier statement
                 ;

part : sphere_action
     | cylinder_action
     | cube_action
     | circle_action
     | square_action
     | polygon_action_nopath
     | polygon_action_plus_path
     | polyhedron_action
     ;

operation : difference_action
          | intersection_action
          | union_action
          | rotate_extrude_action
          | linear_extrude_with_twist
          | rotate_extrude_file
          | import_file1
          | surface_action
          | projection_action
          | hull_action
          | minkowski_action
          ;

multmatrix_action : multmatrix LPAREN matrix RPAREN OBRACE block_list EBRACE ;

group_action1 : group LPAREN RPAREN OBRACE block_list EBRACE ;

group_action2 : group LPAREN RPAREN SEMICOL ;

color_action  : color LPAREN vector RPAREN OBRACE block_list EBRACE ;

render_action : render LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;

not_supported : glide LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE
              | offset LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE
              | resize LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE
              | subdiv LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE
              ;

anymodifier : MODIFIERBACK
            | MODIFIERDEBUG
            | MODIFIERROOT
            | MODIFIERDISABLE
            ;

sphere_action   : sphere LPAREN keywordargument_list RPAREN SEMICOL ;
cylinder_action : cylinder LPAREN keywordargument_list RPAREN SEMICOL ;
cube_action     : cube LPAREN keywordargument_list RPAREN SEMICOL ;
circle_action   : circle LPAREN keywordargument_list RPAREN SEMICOL ;
square_action   : square LPAREN keywordargument_list RPAREN SEMICOL ;
polygon_action_nopath    : polygon LPAREN points EQ OSQUARE points_list_2d ESQUARE COMMA paths EQ undef
                            COMMA keywordargument_list RPAREN SEMICOL ;

polygon_action_plus_path : polygon LPAREN points EQ OSQUARE points_list_2d ESQUARE COMMA paths EQ
                            OSQUARE path_set ESQUARE COMMA keywordargument_list RPAREN SEMICOL ;

polyhedron_action : polyhedron LPAREN points EQ OSQUARE points_list_3d ESQUARE COMMA faces EQ OSQUARE points_list_3d ESQUARE COMMA keywordargument_list RPAREN SEMICOL
                  | polyhedron LPAREN points EQ OSQUARE points_list_3d ESQUARE COMMA triangles EQ OSQUARE points_list_3d ESQUARE COMMA keywordargument_list RPAREN SEMICOL
                   ;

difference_action   : difference LPAREN RPAREN OBRACE block_list EBRACE ;
intersection_action : intersection LPAREN RPAREN OBRACE block_list EBRACE ;
union_action        : union LPAREN RPAREN OBRACE block_list EBRACE ;

rotate_extrude_action : rotate_extrude LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;
linear_extrude_with_twist : linear_extrude LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;
rotate_extrude_file : rotate_extrude LPAREN keywordargument_list RPAREN SEMICOL ;

import_file1        : import LPAREN keywordargument_list RPAREN SEMICOL ;
surface_action      : surface LPAREN keywordargument_list RPAREN SEMICOL ;

projection_action   : projection LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;
hull_action         : hull LPAREN RPAREN OBRACE block_list EBRACE ;

minkowski_action    : minkowski LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;

matrix              : OSQUARE vector COMMA vector COMMA vector COMMA vector ESQUARE ;

vector              : OSQUARE NUMBER COMMA NUMBER COMMA NUMBER COMMA NUMBER ESQUARE ;

keywordargument_list : keywordargument
                     | keywordargument_list COMMA keywordargument
                     ;

boolean : true
        | false
        ;

stripped_string : STRING ;

point_2d : OSQUARE NUMBER COMMA NUMBER ESQUARE ;

points_list_2d : point_2d COMMA
               | points_list_2d point_2d COMMA
               | points_list_2d point_2d
               ;

point_3d : OSQUARE NUMBER COMMA NUMBER COMMA NUMBER ESQUARE ;

points_list_3d : point_3d COMMA
               | points_list_3d point_3d COMMA
               | points_list_3d point_3d
               ;


path_points : NUMBER COMMA
            | path_points NUMBER COMMA
            | path_points NUMBER
            ;

path_list : OSQUARE path_points ESQUARE ;

path_set : path_list
         | path_set COMMA path_list
         ;

size_vector : OSQUARE NUMBER COMMA NUMBER COMMA NUMBER ESQUARE ;

keywordargument : ID EQ boolean
                | ID EQ NUMBER
                | ID EQ size_vector
                | ID EQ vector
                | ID EQ point_2d
                | ID EQ stripped_string
                ;

%%