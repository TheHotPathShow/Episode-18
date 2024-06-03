use std::ffi::{c_char, CStr, CString};
use tree_sitter_highlight::{Highlighter, HighlightEvent};
use tree_sitter_highlight::HighlightConfiguration;
use tree_sitter_highlight::HtmlRenderer;

static HIGHLIGHT_NAMES: &[&str] = &[
    "attribute",
    "constant",
    "comment",
    "constructor",
    "constant.builtin",
    "function",
    "function.builtin",
    "keyword",
    "operator",
    "property",
    "punctuation",
    "string",
    "type",
    "type.builtin",
    "variable.local",
    "variable.builtin",
    "variable.parameter",
    "module",
    "number",
];

static HIGHLIGHT_COLORS: &[&str] = &[
    "#9a7fed",
    "#aa44ff",
    "#84c26a",
    "#38A256",
    "#518fc7",
    "#38A256",
    "#38A256",
    "#518fc7",
    "white",
    "#42a1c5",
    "white",
    "#c9a26d",
    "#9a7fed",
    "#518fc7",
    "#bdbdbd",
    "#bdbdbd",
    "#bdbdbd",
    "#9a7fed",
    "#db7c77",
];

static CLASSES: &[&str] = &[
    "class=\"attribute\"",
    "class=\"constant\"",
    "class=\"comment\"",
    "class=\"constructor\"",
    "class=\"constant_builtin\"",
    "class=\"function\"",
    "class=\"function_builtin\"",
    "class=\"keyword\"",
    "class=\"operator\"",
    "class=\"property\"",
    "class=\"punctuation\"",
    "class=\"string\"",
    "class=\"type\"",
    "class=\"type.builtin\"",
    "class=\"variable\"",
    "class=\"variable_builtin\"",
    "class=\"variable_parameter\"",
    "class=\"type\"",
    "class=\"number\"",
];

#[repr(C)]
pub struct CodeSnippets {
    snippets: *const *const c_char,
    snippet_count: u32,
}

#[no_mangle]
pub unsafe extern "C" fn highlight_c_sharp(source_raw: *const c_char) -> CodeSnippets {
    let source = CStr::from_ptr(source_raw).to_bytes();
    let mut c_sharp_config = HighlightConfiguration::new(
        tree_sitter_c_sharp::language(),
        "c_sharp",
        HIGHLIGHTS_QUERY,
        "",
        LOCALS_QUERY,
    ).unwrap();

    c_sharp_config.configure(&HIGHLIGHT_NAMES);
    let mut binding = Highlighter::new();
    let highligts
        = binding
        .highlight(&c_sharp_config, source, None, |_| None)
        .unwrap();

    let mut renderer = HtmlRenderer::new();
    renderer.render(highligts, source, &|attr| {
        CLASSES[attr.0].as_bytes()
    }).unwrap();

    let raw_lines = renderer.lines()
        .map(|x| CString::new(x).unwrap().into_raw()).collect::<Vec<_>>();
    let len = raw_lines.len() as u32;
    let boxed = raw_lines.into_boxed_slice();
    return CodeSnippets {
        snippets: Box::into_raw(boxed) as *const _,
        snippet_count: len,
    };
}

#[no_mangle]
pub unsafe extern "C" fn highlight_c_sharp_v2(source_raw: *const c_char) -> *const c_char {
    let highlight_query = CString::new(HIGHLIGHTS_QUERY_B).unwrap();
    let locals_query = CString::new(LOCALS_QUERY_B).unwrap();
    return highlight_c_sharp_v2_manual(source_raw, highlight_query.as_ptr(), locals_query.as_ptr());
}
#[no_mangle]
pub unsafe extern "C" fn highlight_c_sharp_v2_manual(source_raw: *const c_char, highlight_query_raw: *const c_char, locals_query_raw: *const c_char) -> *const c_char {
    let source = CStr::from_ptr(source_raw).to_bytes();
    let mut c_sharp_config = HighlightConfiguration::new(
        tree_sitter_c_sharp::language(),
        "c_sharp",
        CStr::from_ptr(highlight_query_raw).to_str().unwrap(),
        "",
        CStr::from_ptr(locals_query_raw).to_str().unwrap(),
    ).unwrap();

    c_sharp_config.configure(&HIGHLIGHT_NAMES);
    let mut binding = Highlighter::new();
    let highlighter
        = binding
        .highlight(&c_sharp_config, source, None, |_| None)
        .unwrap();

    // let mut highlights = Vec::new();
    let mut str_builder = String::new();
    str_builder.push_str("<color=#000000>");
    for event in highlighter {
        match event {
            Ok(HighlightEvent::HighlightStart(s)) => {
                // highlights.push(s);
                // println!("Highlight start: {}", HIGHLIGHT_NAMES[s.0]);
                str_builder.push_str(&format!("<color={}>", HIGHLIGHT_COLORS[s.0]));
            }
            Ok(HighlightEvent::HighlightEnd) => {
                // highlights.pop();
                // println!("Highlight end");
                str_builder.push_str("</color>");
            }
            Ok(HighlightEvent::Source { start, end }) => {
                let source = std::str::from_utf8(&source[start..end]).unwrap();
                // let highlights = highlights.iter().map(|x| HIGHLIGHT_NAMES[x.0]).collect::<Vec<_>>();
                // println!("Source: `{}` - `{}`", source, highlights.join(", "));
                str_builder.push_str(source.replace("<", "&lt;").replace(">", "<b></b>><b></b>").replace("&lt;", "<b></b><<b></b>").as_str());
            }
            Err(a) => println!("Error: {:?}", Err::<(), tree_sitter_highlight::Error>(a)),
        }
    }
    str_builder.push_str("</color>");

    return CString::new(str_builder).unwrap().into_raw();
}

/// The syntax highlighting query for this language.
pub const HIGHLIGHTS_QUERY: &str = include_str!("../queries/highlights.scm");
pub const LOCALS_QUERY: &str = include_str!("../queries/locals.scm");
pub const HIGHLIGHTS_QUERY_B: &[u8] = include_bytes!("../queries/highlights.scm");
pub const LOCALS_QUERY_B: &[u8] = include_bytes!("../queries/locals.scm");

#[cfg(test)]
mod tests {
    use std::ffi::CStr;
    use std::{slice};

    #[test]
    fn test_can_load_grammar() {
        unsafe {
            let res = super::highlight_c_sharp(c"public class DoesItWork : MonoBehaviour
{
    void Start()
    {
        var customer = new Customer();
        foreach (var line in customer.Report())
        {
            Debug.Log(line);
        }
        TreeSitterWrapper.Highlight(\"using System;\");
        var strBuilder = new System.Text.StringBuilder();
    }
}".as_ptr());
            slice::from_raw_parts(res.snippets, res.snippet_count as usize).iter().for_each(|&x| {
                let source = CStr::from_ptr(x).to_str().unwrap();
                println!("{}", source);
            });
        }
    }

    #[test]
    fn manual_highlight() {
        let source = c"public class DoesItWork : MonoBehaviour
{
    void Start()
    {
        var customer = new Customer();
        foreach (var line in customer.Report())
        {
            Debug.Log(line)
        }
        TreeSitterWrapper.Highlight(\"<>\");
        var str_builder = new System.Text.StringBuilder();
    }
}".as_ptr();
        unsafe {
            let default = super::highlight_c_sharp_v2(source);
            println!("{}", CStr::from_ptr(default).to_str().unwrap());
        }
    }
}
