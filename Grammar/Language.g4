grammar Language;

expr: expr OPERATOR expr | INT | '(' expr ')' ;

OPERATOR: '+' | '-' | '*' | '/' | '%' ;
INT: [0-9]+ ;
DECIMAL_INT: '0' | ( [+\-]? [1-9][0-9]* ) ;
HEX_INT: '0x' [0-9a-fA-F]+ ;
VARNAME: [a-zA-Z_][a-zA-Z_0-9]* ;
// WS : [ \t\r\n]+ -> skip ;
