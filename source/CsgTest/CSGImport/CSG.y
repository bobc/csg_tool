/* OpenSCAD CSG grammar derived from FreeCAD:importCSG.py 
 *
 * Designed for Gardens Point Yacc-like parser generator https://gppg.codeplex.com/
 *    
 * Parser is generated from command line :
 * > gppg /nolines CSG.y
 */

%namespace CSGImport
%output=CSGParse.cs 
%partial 
//%visibility internal

%YYSTYPE CSGImport.Node

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
%token _true
%token _false
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
%token hull
%token minkowski
%token projection
%token import
%token color
%token offset
%token resize

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
%token MODIFIER_BACK
%token MODIFIER_DEBUG
%token MODIFIER_ROOT
%token MODIFIER_DISABLE

%%

module      : block_list                   { StoreResult ($1); } ;

block_list  : statement                    { $$ = $1; }
            | block_list statement         { $$ = MakeBinary (NodeTag.blocklist, $2, $1); }
            | statementwithmod             { $$ = $1; }
            | block_list statementwithmod  { $$ = MakeBinary (NodeTag.blocklist, $2, $1); }
            ;

statement   : part                         { $$ = $1; }
            | operation                    { $$ = $1; }
            | multmatrix_action            { $$ = $1; }
            | group_action1                { $$ = $1; }
            | group_action2                { $$ = $1; }
            | color_action                 { $$ = $1; }
            | render_action                { $$ = $1; }
            | offset_action                { $$ = $1; }
            | resize_action                { $$ = $1; }
            ;

statementwithmod : anymodifier statement   { $$ = $2; }
                 ;

anymodifier : MODIFIER_BACK
            | MODIFIER_DEBUG
            | MODIFIER_ROOT
            | MODIFIER_DISABLE
            ;


part        : sphere_action                 { $$ = $1; }
            | cylinder_action               { $$ = $1; }
            | cube_action                   { $$ = $1; }
            | circle_action                 { $$ = $1; }
            | square_action                 { $$ = $1; }
            | polygon_action_nopath         { $$ = $1; }
            | polygon_action_plus_path      { $$ = $1; }
            | polyhedron_action             { $$ = $1; }
            ;

operation   : difference_action             { $$ = $1; }
            | intersection_action           { $$ = $1; }
            | union_action                  { $$ = $1; }
            | rotate_extrude_action         { $$ = $1; }
            | linear_extrude_with_twist     { $$ = $1; }
            | rotate_extrude_file           { $$ = $1; }
            | import_file1                  { $$ = $1; }
            | surface_action                { $$ = $1; }
            | projection_action             { $$ = $1; }
            | hull_action                   { $$ = $1; }
            | minkowski_action              { $$ = $1; }
            ;

/* statement */
multmatrix_action: multmatrix LPAREN matrix RPAREN OBRACE block_list EBRACE               { $$ = MakeBinary (NodeTag.multmatrix, $3, $6); } ;

group_action1   : group LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE       { $$ = MakeBinary (NodeTag.group, $3, $6); } ;

group_action2   : group LPAREN keywordargument_list RPAREN SEMICOL                        { $$ = MakeBinary (NodeTag.group, null, null); } ;

color_action    : color LPAREN vector RPAREN OBRACE block_list EBRACE                     { $$ = MakeBinary (NodeTag.color, $3, $6); };

render_action   : render LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;

offset_action   : offset LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;

resize_action   : resize LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;


/* part */
sphere_action   : sphere   LPAREN keywordargument_list RPAREN SEMICOL   { $$ = MakeBinary (NodeTag.sphere, $3, null); } ;
cylinder_action : cylinder LPAREN keywordargument_list RPAREN SEMICOL   { $$ = MakeBinary (NodeTag.cylinder, $3, null); };
cube_action     : cube     LPAREN keywordargument_list RPAREN SEMICOL   { $$ = MakeBinary (NodeTag.cube, $3, null); } ;
circle_action   : circle   LPAREN keywordargument_list RPAREN SEMICOL   ;
square_action   : square   LPAREN keywordargument_list RPAREN SEMICOL   ;

polygon_action_nopath : polygon LPAREN points EQ OSQUARE points_list_2d ESQUARE COMMA paths EQ undef
                            COMMA keywordargument_list RPAREN SEMICOL ;

polygon_action_plus_path : polygon LPAREN points EQ OSQUARE points_list_2d ESQUARE COMMA paths EQ
                            OSQUARE path_set ESQUARE COMMA keywordargument_list RPAREN SEMICOL ;

polyhedron_action : polyhedron LPAREN points EQ OSQUARE points_list_3d ESQUARE COMMA faces EQ OSQUARE points_list_3d ESQUARE COMMA keywordargument_list RPAREN SEMICOL
                  | polyhedron LPAREN points EQ OSQUARE points_list_3d ESQUARE COMMA triangles EQ OSQUARE points_list_3d ESQUARE COMMA keywordargument_list RPAREN SEMICOL
                   ;

/* operation */
difference_action   : difference LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE        { $$ = MakeBinary (NodeTag.difference, $3, $6); } ;
intersection_action : intersection LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE      { $$ = MakeBinary (NodeTag.intersect, $3, $6); }  ;
union_action        : union LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE             { $$ = MakeBinary (NodeTag.union, $3, $6); }      ;

rotate_extrude_action : rotate_extrude LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;
linear_extrude_with_twist : linear_extrude LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;
rotate_extrude_file : rotate_extrude LPAREN keywordargument_list RPAREN SEMICOL ;

import_file1        : import LPAREN keywordargument_list RPAREN SEMICOL ;
surface_action      : surface LPAREN keywordargument_list RPAREN SEMICOL ;

projection_action   : projection LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;
hull_action         : hull LPAREN RPAREN OBRACE block_list EBRACE ;

minkowski_action    : minkowski LPAREN keywordargument_list RPAREN OBRACE block_list EBRACE ;


/* parameter values */
matrix              : OSQUARE vector COMMA vector COMMA vector COMMA vector ESQUARE { $$ = MakeLeafMatrix ($2, $4, $6, $8); } ;

vector              : OSQUARE NUMBER COMMA NUMBER COMMA NUMBER COMMA NUMBER ESQUARE { $$ = MakeLeafVector ($2, $4, $6, $8); } ; 

size_vector         : OSQUARE NUMBER COMMA NUMBER COMMA NUMBER ESQUARE      { $$ = MakeLeafVector ($2, $4, $6); } ;

point_3d            : OSQUARE NUMBER COMMA NUMBER COMMA NUMBER ESQUARE      { $$ = MakeLeafVector ($2, $4, $6); } ;

point_2d            : OSQUARE NUMBER COMMA NUMBER ESQUARE                   { $$ = MakeLeafVector ($2, $4); } ;


boolean         : _true      { $$ = MakeIdLeaf("true"); }
                | _false     { $$ = MakeIdLeaf("false"); }
                ;

stripped_string : STRING ;

keywordargument_list : keywordargument                                      { $$ = MakeBinary (NodeTag.arglist, $1, null); }
                     | keywordargument_list COMMA keywordargument           { $$ = MakeBinary (NodeTag.arglist, $3, $1); }
                     |
                     ;

keywordargument : ID EQ boolean             { $$ = MakeBinary (NodeTag.arg, $1, $3); }
                | ID EQ NUMBER              { $$ = MakeBinary (NodeTag.arg, $1, $3); }
                | ID EQ size_vector         { $$ = MakeBinary (NodeTag.arg, $1, $3); }
                | ID EQ vector              { $$ = MakeBinary (NodeTag.arg, $1, $3); }
                | ID EQ point_2d            { $$ = MakeBinary (NodeTag.arg, $1, $3); }
                | ID EQ stripped_string     { $$ = MakeBinary (NodeTag.arg, $1, $3); }
                ;


points_list_2d  : point_2d COMMA
                | points_list_2d point_2d COMMA
                | points_list_2d point_2d
                ;

points_list_3d  : point_3d COMMA
                | points_list_3d point_3d COMMA
                | points_list_3d point_3d
                ;

path_points     : NUMBER COMMA
                | path_points NUMBER COMMA
                | path_points NUMBER
                ;

path_list       : OSQUARE path_points ESQUARE ;

path_set        : path_list
                | path_set COMMA path_list
                ;


%%