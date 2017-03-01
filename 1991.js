/*jslint browser: true*/
(function () {
    // Used for giving the page a bit more contrast / interest / edge.
    var pretags = Array.prototype.slice.call(document.querySelectorAll("pre"));

    function createTab (pre, tabs) {
        var lines = pre.innerHTML.split("\n");
        var tabname = lines[0].replace(/\\\s+?/, "");
        pre.innerHTML = lines.slice(1).join("\n");
        pre.setAttribute("data-tab-name", tabname);
        var tab = document.createElement("div");
        tab.innerHTML = tabname;
        tab.onclick = function () {
            var browser = this.parentElement.parentElement;
            var selected = browser.querySelectorAll(".selected");
            selected[0].className = selected[1].className = "";
            var file = browser.querySelector("[data-tab-name='" + tabname + "']");
            file.className = this.className = "selected";
        };
        tabs.appendChild(tab);
    }
    function syntactify (type, content) {
        return "<span class='" + type + "'>" + content + "</span>";
    }
    function parseForth (pre) {
        var content = pre.innerHTML;
        // Keywords:
        content = content.replace(/( (;|1991:)(\s+?|$))|(\/1991 )|(include )|(s\+|\$type|loop|do)/g, function (match) {
            return syntactify("keyword", match);
        }).replace(/(\n|>)(: \S+)/g, function (match, p1, p2) {
            // Special case of word definitions.
            return p1 + syntactify("keyword", p2);
        });
        // Strings:
        content = content.replace(/s" .*"/g, function (match) {
            return syntactify("string", match);
        });
        // Numbers:
        content = content.replace(/(>|\s+)(\d+)(<|\s+)/g, function (match, p1, p2, p3) {
            return p1 + syntactify("number", p2) + p3;
        }).replace(/(>|\s+)(\d+)(<|\s+)/g, function (match, p1, p2, p3) {
            return p1 + syntactify("number", p2) + p3;
        }).replace(/&lt;# #s #&gt;/g, function (match) {
            return syntactify("number", match);
        });
        // Comments:
        content = content.replace(/\\\ .*/g, function (match) {
            return syntactify("comment", match);
        }).replace(/\( .* \)/g, function (match) {
            return syntactify("comment", match);
        });
        pre.innerHTML = content;
    }
    function parseHTML (pre) {
        var content = pre.innerHTML;
        // Replace html closing brace.
        content = content.replace(/>/g, "&gt;");

        // DOCTYPE
        content = content.replace(/&lt;!DOCTYPE html&gt;/, function (match) {
            return syntactify("comment", match);
        });

        // Keyword wrappers
        content = content.replace(/\$?(&(lt|gt);(\/|\$)?)/g, function (match) {
            return syntactify("keyword-wrapper", match);
        });

        // Keywords
        content = content.replace(/(>)(html|head|body|code|title)(<)/g, function (match, p1, p2, p3) {
            return p1 + syntactify("keyword", p2) + p3;
        }).replace(/(>)(meta)( )/g, function (match, p1, p2, p3) {
            return p1 + syntactify("keyword", p2) + p3;
        });
        pre.innerHTML = content;
    }
    function parseSyntax (pre) {
        var ft = pre.getAttribute("data-tab-name").split(".").slice(-1)[0];
        if (ft === "fs") {
            parseForth(pre);
        } else if (ft === "html") {
            parseHTML(pre);
        }
    }
    function convertToBrowser (div) {
        div.className = "browser";
        var tabs = document.createElement("div");
        tabs.className = "tabs";
        Array.prototype.forEach.call(div.children, function (pre) {
            createTab(pre, tabs);
            parseSyntax(pre);
        });
        div.children[0].className = tabs.children[0].className = "selected";
        div.insertBefore(tabs, div.children[0]);
    }

    function collectPre (siblings) {
        var sibling = siblings.slice(-1)[0];
        while (sibling) {
            sibling = sibling.nextSibling;
            if (sibling && sibling.nodeType === 1) {
                if (sibling.nodeName.toLowerCase() !== "pre") {
                    sibling = null;
                } else {
                    break;
                }
            }
        }
        if (sibling) {
            sibling.parsed = true;
            siblings.push(sibling);
            return collectPre(siblings);
        }
        return siblings;
    }

    pretags.forEach(function (pre) {
        if (!pre.parsed) {
            var div = document.createElement("div");
            pre.parentElement.insertBefore(div, pre);
            var allPre = collectPre([pre]);
            allPre.forEach(function (p) {
                div.appendChild(p);
            });
            pre.parsed = true;
            convertToBrowser(div);
        }
    });
}());
