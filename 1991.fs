\ 1991

include unix/socket.fs

: read-request ( socket -- addr u ) pad 4096 read-socket ;

: send-response ( addr u socket -- )
    dup >R write-socket R> close-socket ;

: start-server { server client }
    begin
        server 255 listen
        server accept-socket to client

        client read-request type s\" HTTP/1.1 200 OK\n Content-Type: text/html\n\n fffff" client send-response
    again ;

: 1991: ( port -- ) create-server 0 start-server ;
: 1991/ ( "<path> <word>" -- )
    \ TODO store each path => xt and execute within
    \ handle-server
    bl word
    cr ." Setting get for " count type
    \ TODO handle non-words. Should give the user
    \ some compile-/run-time error.
    ' \ fetch xt
    dup >name
    cr ." handler word is " name>string type
    cr ." running handler: " execute ;


\ App demo:
: handle-hi ." hi!" ; \ not sure printing is the way to go?

1991/ hi handle-hi

\ 8080 1991:
