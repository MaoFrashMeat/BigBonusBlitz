var fso = new ActiveXObject("Scripting.FileSystemObject");
var f = fso.OpenTextFile("d:\\GitHub\\BigBonusBlitz\\settings.js", 1);
var content = f.ReadAll();
f.Close();

// Parse the JSON. Wait, cscript doesn't have JSON object by default in JScript unless it's HTMLfile.
// Instead of parsing, we can just replace the string.
content = content.replace('"probabilities": {', '"probabilities_A": {');

// Write back
var f2 = fso.OpenTextFile("d:\\GitHub\\BigBonusBlitz\\settings.js", 2);
f2.Write(content);
f2.Close();
