\ 1991

include unix/socket.fs

\ Helper words
: +s ( addr1 u1 addr2 u2 -- addr3 u3 )          \ like s+ but prepend rather than append.
    2swap s+ ;
: str-count ( addr1 u1 addr2 u2 -- u3 )         \ Counts occurrences of addr2 within addr1.
    2swap 0 >r
    begin 2over search
    while 2over nip /string
        r> 1+ >r
    repeat 2drop 2drop r> ;
: -leading ( addr len -- addr' len' )
    begin over c@ bl = while 1 /string repeat ;
: trim ( addr len -- addr' len')
    -leading -trailing ;
: exchange ( a1 a2 -- )
    2dup c@ swap c@ rot c! swap c! ;
: reverse ( caddr u -- )                        \ reverse a string
    1- bounds begin 2dup > while
        2dup exchange
    -1 /string
    repeat 2drop ;
: sourcedir ( -- saddr su )
    \ Returns the directory in which the file
    \ invoking the word finds itself
    \ relative to gforth's execution directory.
    \ Useful for specifying in which dir to find
    \ specific files (e.g., public/, views/).
    sourcefilename                              \ get the name of our file
    pad dup >r place                            \ copy the string so we don't
    r> count                                    \       modify sourcefilename.
    2dup reverse                                \ reverse and search for first /
    s" /" search if                             \ if found, reverse string to
            2dup reverse                        \ strip the filename but keep dir.
        else
            2drop                               \ no slash,
            s" ./"                              \ same dir execution.
        then ;
: file-exists? ( addr u -- addr u bool )
    2dup file-status nip 0= ;
: pubvar create 0 , 0 , ;

\ Query params
pubvar queryString
: set-query-string ( addr u -- )
    queryString 2! ;
: get-query-string ( -- addr u )
    queryString 2@ ;
: add-to-query-string ( addr u -- )
    get-query-string
    dup if                                      \ If queryString isn't empty, add & before
        s" &" s+
    then
    +s                                          \ adding our new query values.
    set-query-string ;

pubvar tmpQueryString
: set-tmp-query-string ( addr u -- )
    tmpQueryString 2! ;
: get-tmp-query-string ( -- addr u )
    tmpQueryString 2@ ;
: add-to-tmp-query-string ( addr u -- )
    get-tmp-query-string
    dup if                                      \ If queryString isn't empty, add & before
        s" &" s+
    then
    +s                                          \ adding our new query values.
    set-tmp-query-string ;

\ Request body
pubvar requestBody
: set-request-body ( addr u -- )
    requestBody 2! ;
: get-request-body ( -- addr u )
    requestBody 2@ ;

\ Request method
pubvar requestMethod
: set-request-method ( addr u -- )
    requestMethod 2! ;
: get-request-method ( -- addr u )
    requestMethod 2@ ;

\ User-defined routing
wordlist constant routes
pubvar reqroute
: set-requested-route ( addr u -- )
    reqroute 2! ;
: get-requested-route ( -- addr u )
    reqroute 2@ ;
: fuzzy-find-route ( xt -- xt' bool )
    \ Takes an xt that accepts a name token
    \ and returns a bool.
    \ Traverse will run as long as xt
    \ returns true.
    \ Also takes the addr u of the requested
    \ route we're trying to validate.
    >r routes wordlist-id @                     \ Store xt and specify wordlist
    begin
    dup
    while
        r@ over >r execute while r> @
    repeat r>
    then
    rdrop
    ?dup if
        get-tmp-query-string                    \ Save our fuzzy vars to the request's
        add-to-query-string                     \ query real string.

        name>int                                \ Get the xt of the nt.
        -1                                      \ Return true.
    else
        0
    then ;
: fuzzy-compare ( nt -- bool )
    \ Takes a route name token and returns
    \ whether that route name fuzzy matches
    \ the requested url
    s" " set-tmp-query-string                   \ Reset tmp query string.
    name>string                                 \ Get the string value of the NT.
    2dup s" <" search if                        \ See if the route expects fuzzy matching.
        2drop                                   \ Drop search results.
        2dup s" /" str-count >r                 \ Check to see if both routes have the same
        get-requested-route s" /" str-count     \ number of / occurrences.
        r> = if
            2dup s" <" str-count 0 do
                2dup 2>r
                2r@
                2dup s" <" search drop          \ Get position of "<",
                nip -
                nip
                get-requested-route rot /string \ crop until there in the requested route,
                2dup s" /" search if            \ search for the next / or end of route,
                    nip -
                else
                    2drop
                then
                \ (
                2dup 2r> 2swap 2>r 2>r          \ (Store a copy of the real value of <match>.)
                \ )
                2r@
                2dup s" <" search drop          \ and replace <...> with the requested route word
                nip
                \ (
                2dup - 1+ 2r@ rot /string       \ (Store the beginnings of user's <"match"> word.)
                2dup s" >" search drop          \ (Retrieve full <"match"> user word,)
                nip - s" =" s+
                2r> 2r> 2swap 2>r
                s+                              \ (and associate it with the request value,)
                add-to-tmp-query-string         \ (before saving it to the tmp query string.)
                \ )
                -
                +s                              \ by prepending pre-< to route word
                2r> s" >" search drop 1 /string \ and then by appending post-> to route word.
                s+
                2>r                             \ Save string progress,
                2drop                           \ drop old string,
                2r>                             \ set new string for start of next loop (or end).
            loop
            get-requested-route compare         \ Check to see if the strings match.
        else
            2drop                               \ Drop name>string.
            -1                                  \ Keep looping.
        then
    else
        2drop                                   \ Drop search results.
        2drop                                   \ Drop name>string.
        -1                                      \ Keep looping.
    then ;
: fuzzy-match ( addr u -- xt bool )
    set-requested-route
    ['] fuzzy-compare fuzzy-find-route ;
: find-route ( addr u -- data )
    2dup 2>r
    routes search-wordlist if
        2rdrop                                  \ Exact match found. Drop the dup string.
        >body @
    else
        2r>
        fuzzy-match if                          \ Fuzzy match found.
            >body @
        else
            0                                   \ No match at all.
        then
    then ;
: register-route ( data addr u -- )
    2dup routes search-wordlist if
        routes drop nip nip
        >body !
    else
        routes get-current >r set-current       \ switch definition word lists
        nextname create ,
        r> set-current
    then ;

\ Public directory
pubvar public
: set-public-path ( addr u -- )
    public 2! ;
: get-public-path ( -- addr u )
    public 2@ ;
sourcedir s" public" s+ set-public-path

\ Views directory
pubvar views
: set-view-path ( addr u -- )
    views 2! ;
: get-view-path ( -- addr u )
    views 2@ ;
sourcedir s" views/" s+ set-view-path           \ Needs that trailing slash

\ Handling views
pubvar viewoutput
: set-view-output ( addr u -- )
    viewoutput 2! ;
: get-view-output ( -- addr u )
    viewoutput 2@ ;
: parse-view ( addr u -- )
    \ Get string between <$ $> and invoke `evaluate`.
    \ Append to viewoutput as we go.
    \ There's probably a better way of doing this
    \ but it works for me.
    begin
    2dup s" <$" search if
            2over swap 2>r                      \ If there is a match for <$, save addr and u.
            swap >r dup >r                      \ Store the match and output anything that
            -                                   \ comes before it.
            get-view-output +s
            set-view-output
            r> r> swap                          \ Then reinstate the match "<$...".
            2dup s" $>" search if               \ Check to see if there's a closing tag $>.
                2 -                             \ Add the close tag to the search result.
                nip                             \ Save the end position of $>.
                dup >r
                -                               \ Reduce the string to <$ ... $>.
                evaluate                        \ Run user's code (maybe a bad idea?).
                r>                              \ Retrieve our saved end position of $>.
                r> r>                           \ Retrive the addr u from start of loop iter.
                rot                             \ Bring end $> to stack top.
                over >r                         \ Store the real string's length.
                -                               \ Subtract end $> from u to get the pos from top
                r> swap                         \ that we'd like to strip away. Restore saved u.
                /string                         \ Drop top of the string until the end of $>.
                0                               \ Keep looping.
            else
                get-view-output +s              \ No closing tag. Just save the full string.
                set-view-output
                2rdrop                          \ And drop the stored addr and u
                2drop                           \ as well as both the 2dup we made before
                2drop                           \ searching twice.
                -1                              \ Exit the loop.
            then
        else
            2drop                               \ No match for <$. Drop the 2dup from before search.
            get-view-output +s
            set-view-output                     \ Save string as-is to view output
            -1                                  \ exit the loop
        then
    until ;
: render-view ( addr u -- vaddr vu )            \ Accepts a view filename. Returns parsed contents.
    s" " set-view-output
    get-view-path +s
    file-exists? if
            slurp-file
            parse-view
        else
            exit                                \ Continue to 404, no view.
        then
    get-view-output ;
: <$ ( -- ) ;                                   \ Do nothing.
: $> ( -- ) ;                                   \ Do nothing.
: $type ( addr u -- )                           \ User-land word for outputing via views.
    get-view-output +s
    set-view-output ;
: import ( -- )                                 \ User-land word for including other view files.
    get-view-path +s
    file-exists? if
            slurp-file
            parse-view
        else
            s" "
        then ;

\ Request's Content-Type
pubvar RequestContentType
: set-content-type ( addr u -- )
    RequestContentType 2! ;
: get-content-type ( -- addr u )
    RequestContentType 2@ ;

: filetype: ( addr u "extension" -- )           \ takes a content-type and the extension
    create here over 1+ allot place
    does> count ;

: get-filetype ( addr u -- caddr cu )           \ takes an extension, looks to find a definition
    find-name dup if
        name>int
        execute
    else
        drop
        s" text/plain"
    then ;

s" text/plain" filetype: txt                    \ txt should always be defined
s" text/html" filetype: html
s" text/css" filetype: css
s" text/javascript" filetype: js
s" image/png" filetype: png
s" image/gif" filetype: gif
s" image/jpeg" filetype: jpg
s" image/jpeg" filetype: jpeg
s" image/x-icon" filetype: ico


\ Internal request handling
: HTTP/1.1 s" HTTP/1.1 " ;

: response-status ( u -- addr u )
    dup case                                    \ get status code info
        200 of s"  OK" endof
        404 of s"  Not Found" endof
    endcase
    s\" \n" s+
    rot s>d <# #s #> +s                         \ convert status code to string and prepend to info
    HTTP/1.1 +s ;                               \ prepend HTTP/1.1

: content-type ( addr u -- caddr cu )
    s" Content-Type: " +s                       \ Prepend to the provided content type
    s\" \n\n" s+ ;                              \ Append 2 new lines

: set-header ( u addr u -- raddr ru )           \ Accepts status code and content type string
    rot response-status                         \ Set response status
    2swap content-type                          \ Set content-type
    s+ ;                                        \ Join

: get-content-length ( addr u -- clen )
    s" Content-Length:" search if
        2dup s\" \n" search drop nip -
        s" Content-Length:" nip /string
        trim
        s>number?
        2drop
    else
        2drop
        0
    then ;

: read-request-body ( socket u -- )
    \ Takes the socket and the length of the
    \ body (Content-Length).
    here swap aligned read-socket
    set-request-body ;
: read-request ( socket -- addr u )
    \ Returns the request header
    \ but also collects the request body.
    dup >r
    pad 4096 read-socket
    r> dup 2over rot drop
    get-content-length ?dup if
        read-request-body
    else
        s" " set-request-body
        drop
    then ;

: send-response ( addr u socket -- )
    dup >r write-socket r> close-socket ;

: store-query-string ( addr u -- raddr ru )
    2dup s" ?" search if
        2dup 1 /string set-query-string         \ Store query string (without leading "?").
        nip -
    else
        s" " set-query-string                   \ Store empty query string (reset).
        2drop
    then ;

: requested-route ( addr u -- raddr ru )
    bl scan 1- swap 1+ swap
    2dup bl scan nip -                          \ get the space-separated route
    store-query-string ;                        \ strip and store the query, leave route

: .extension ( addr u -- addr u )
    2dup reverse                                \ reverse the file name
    2dup s" ." search                           \ search for the first occurance of "."
    if
        nip -                                   \ remove the "." from the search results
    else
        s" txt"
    then
    2dup reverse ;                              \ reverse reversed extension

: serve-file-type ( addr u -- )
    .extension get-filetype set-content-type ;

: serve-file ( addr u -- addr u )
    slurp-file ;

: 404content-type txt ;
: 404html s" 404" ;

: valid-method? ( addr u -- addr' u' bool )
    2dup s" GET" search if
        2nip
        -1
        s" GET" set-request-method
        exit
    then
    2drop

    2dup s" POST" search if
        2nip
        -1
        s" POST" set-request-method
        exit
    then
    2drop

    2dup s" PUT" search if
        2nip
        -1
        s" PUT" set-request-method
        exit
    then
    2drop

    2dup s" DELETE" search if
        2nip
        -1
        s" DELETE" set-request-method
        exit
    then
    2drop
    0 ;

: either-resolve ( addr u -- resolveaddr resolveu )
    valid-method? if
        s" html" get-filetype set-content-type  \ reset the request's content-type
        requested-route
        2dup find-route dup if
                >r 2drop r>                     \ keep xt, drop the route string
                execute                         \ execute the user's route handler
            else
                drop                            \ drop the null xt
                get-public-path +s              \ see if route exists in public dir
                file-exists? if
                    2dup serve-file             \ collect file contents
                    2swap serve-file-type       \ set the file type
                else
                    exit                        \ continue to 404
                then
            then
        200 get-content-type set-header +s
        rdrop exit
    then ;

: or-404 ( addr u -- 404addr 404u )
    2drop
    404 404content-type set-header 404html s+ ;

: prepare-response ( addr u -- returnaddr returnu)
    either-resolve or-404 ;

: start-server { server client }
    begin
        server 255 listen
        server accept-socket to client

        client read-request prepare-response client send-response
    again ;


\ Userland
: 1991: ( port -- )
    create-server 0 start-server ;
: /1991 ( "<path> <word>" -- )
    bl word ' swap count register-route ;
