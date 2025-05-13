import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage

class AddingShelterPage extends StatelessWidget {
  const AddingShelterPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Stack(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20.0),
              child: SingleChildScrollView(
                child: Column(
                  children: [
                    const SizedBox(height: 20),
                    Image.asset(
                      'assets/images/LOGO1.png', // עדכן את הנתיב לתמונה שלך
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(height: 10),
                    const Text(
                      'הוספת מרחב מוגן',
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 20),
                    _buildTextField('הזן כתובת', 'עיר, רחוב, מספר בית'),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildTextField('שם', 'הבית של דני'),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildTextField('תפוסה מקסימלית', '7'),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildDropdownField('לאפשר כניסת חיות?', ['לא', 'כן']),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildDropdownField('קיימת נגישות?', ['לא', 'כן']),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildTextField('הערות?', '', maxLength: 255),
                    const SizedBox(height: 15), // ריווח קטן יותר לפני הכפתור
                    ElevatedButton(
                      onPressed: () {
                        // הוסף כאן את הפעולה להוספת מרחב מוגן
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color.fromARGB(
                          255,
                          29,
                          46,
                          89,
                        ), // כחול כהה
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 20,
                          vertical: 10,
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const SizedBox(width: 8), // ריווח בין הסמל לטקסט
                          const Text('הוספה', style: TextStyle(fontSize: 16)),
                        ],
                      ),
                    ),
                    const SizedBox(height: 20), // ריווח נוסף מתחת לכפתור
                  ],
                ),
              ),
            ),
            Positioned(
              top: 20,
              right: 20,
              child: IconButton(
                icon: const Icon(Icons.arrow_forward_ios, color: Colors.black),
                onPressed: () {
                  Navigator.pop(context); // חזרה לדף הבית
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTextField(String label, String hint, {int? maxLength}) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
          ), // גודל טקסט קטן יותר
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 40, // גובה קטן יותר לשדה
          child: TextField(
            maxLength: maxLength, // הגבלת מספר תווים
            style: const TextStyle(
              fontSize: 12,
              color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לטקסט
            ),
            decoration: InputDecoration(
              hintText: hint,
              hintStyle: const TextStyle(
                fontSize: 12,
                color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לרמז
              ),
              filled: true,
              fillColor: const Color(0xFFB0C4DE),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
              counterText: '', // הסתרת מונה התווים
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildDropdownField(String label, List<String> options) {
    String? selectedValue = options.first;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
          ), // גודל טקסט קטן יותר
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 40, // גובה קטן יותר לשדה
          child: DropdownButtonFormField<String>(
            value: selectedValue,
            decoration: InputDecoration(
              filled: true,
              fillColor: const Color(0xFFB0C4DE),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
            ),
            style: const TextStyle(
              fontSize: 12,
              color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לטקסט
            ),
            items:
                options
                    .map(
                      (option) => DropdownMenuItem(
                        value: option,
                        child: Text(
                          option,
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color.fromARGB(
                              255,
                              29,
                              46,
                              89,
                            ), // כחול כהה לטקסט
                          ),
                        ),
                      ),
                    )
                    .toList(),
            onChanged: (value) {
              selectedValue = value;
            },
          ),
        ),
      ],
    );
  }
}
