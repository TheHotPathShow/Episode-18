use std::ffi::{c_char, CStr, CString};
use tree_sitter_highlight::{Highlighter, HighlightEvent};
use tree_sitter_highlight::HighlightConfiguration;

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

#[no_mangle]
pub  unsafe extern "C" fn highlight_c_sharp_free(ptr: *const c_char) {
    if ptr.is_null() {
        return;
    }
    let _ = CString::from_raw(ptr as *mut c_char);
}

static mut C_SHARP_CONFIG: Option<HighlightConfiguration> = None;

#[no_mangle]
pub unsafe extern "C" fn highlight_c_sharp_set_defaults() {
    let mut config = HighlightConfiguration::new(
        tree_sitter_c_sharp::language(),
        "c_sharp",
        HIGHLIGHTS_QUERY,
        "",
        LOCALS_QUERY,
    ).unwrap();
    config.configure(&HIGHLIGHT_NAMES);
    C_SHARP_CONFIG = Some(config);
}

#[no_mangle]
pub unsafe extern "C" fn highlight_c_sharp_set_queries(highlight_query_raw: *const c_char, locals_query_raw: *const c_char) {
    let mut config = HighlightConfiguration::new(
        tree_sitter_c_sharp::language(),
        "c_sharp",
        CStr::from_ptr(highlight_query_raw).to_str().unwrap(),
        "",
        CStr::from_ptr(locals_query_raw).to_str().unwrap(),
    ).unwrap();
    config.configure(&HIGHLIGHT_NAMES);
    C_SHARP_CONFIG = Some(config);
}

#[no_mangle]
pub unsafe extern "C" fn highlight_c_sharp(source_raw: *const c_char) -> *const c_char {
    let source = CStr::from_ptr(source_raw).to_bytes();
    let config = C_SHARP_CONFIG.as_ref().unwrap();

    let mut binding = Highlighter::new();
    let highlighter
        = binding
        .highlight(config, source, None, |_| None)
        .unwrap();

    let mut str_builder = String::new();
    let mut escaped_temp_str = String::new();
    // str_builder.push_str("<color=#000000>");
    for event in highlighter {
        match event {
            Ok(HighlightEvent::HighlightStart(s)) => {
                str_builder.push_str(&format!("<color={}>", HIGHLIGHT_COLORS[s.0]));
            }
            Ok(HighlightEvent::HighlightEnd) => {
                str_builder.push_str("</color>");
            }
            Ok(HighlightEvent::Source { start, end }) => {
                let source = std::str::from_utf8(&source[start..end]).unwrap();
                escaped_temp_str.clear();
                escaped_temp_str.reserve(source.len());

                // way one
                let mut current_read = 0;
                let mut consecutive = 0;
                for c in source.as_bytes() {
                    match c {
                        b'<' => {
                            escaped_temp_str.push_str(&source[current_read..current_read +consecutive]);
                            escaped_temp_str.push_str("<b></b><<b></b>");
                            current_read += consecutive;
                            consecutive = 0;
                        },
                        b'>' =>{
                            escaped_temp_str.push_str(&source[current_read..current_read +consecutive]);
                            escaped_temp_str.push_str("<b></b>><b></b>");
                            current_read += consecutive;
                            consecutive = 0;
                        }
                        _ => consecutive += 1,
                    }
                }
                escaped_temp_str.push_str(&source[current_read..current_read +consecutive]);


                str_builder.push_str(escaped_temp_str.as_str());
            }
            Err(a) => println!("Error: {:?}", Err::<(), tree_sitter_highlight::Error>(a)),
        }
    }
    // str_builder.push_str("</color>");

    return CString::new(str_builder).unwrap().into_raw();
}

/// The syntax highlighting query for this language.
pub const HIGHLIGHTS_QUERY: &str = include_str!("../queries/highlights.scm");
pub const LOCALS_QUERY: &str = include_str!("../queries/locals.scm");

#[cfg(test)]
mod tests {
    use std::ffi::CStr;

    #[test]
    fn test_can_load_grammar() {
        unsafe {
            super::highlight_c_sharp_set_defaults();
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
            println!("{}", CStr::from_ptr(res).to_str().unwrap());
        }
    }

    #[test]
    fn manual_highlight() {
        unsafe {
            super::highlight_c_sharp_set_defaults();
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
            let default = super::highlight_c_sharp(source);
            println!("{}", CStr::from_ptr(default).to_str().unwrap());
        }
    }
}
