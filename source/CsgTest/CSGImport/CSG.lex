/* scanner for OpenSCAD CSG files */


%%

"("				return t_LPAREN;
")"	      return t_RPAREN;  
"{"       return t_OBRACE;  
"}"       return t_EBRACE;  
"["       return t_OSQUARE;
"]"       return t_ESQUARE; 
","       return t_COMMA;   
";"       return t_SEMICOL; 
"="       return t_EQ;      
"."       return t_DOT;
"%"       return t_MODIFIERBACK;    
"#"       return t_MODIFIERDEBUG;   
"!"       return t_MODIFIERROOT;    
"*"       return t_MODIFIERDISABLE; 


[$]?[a-zA-Z_]+[0-9]*  												return t_WORD; 
[-]?[0-9]*[\.]*[0-9]+([eE]-?[0-9]+)*  				return t_NUMBER; 
\"[^"]*\"																			return t_STRING;

//[^\r\n]*((\r\n)|<<EOF>>)		/* comment */ ;

//[^\n]*((\n)|<<EOF>>)    		/* comment */ ;

[ \t\r] 			                /* white space */ ;

%%
