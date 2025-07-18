import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'base_page.dart';

// ייבוא חבילות נדרשות

class UpdateKnownLocationPage extends StatefulWidget {
  // קומפוננטה לעריכת אזור מוכר, מקבלת locationData
  final Map<String, dynamic> locationData;
  const UpdateKnownLocationPage({super.key, required this.locationData});

  @override
  State<UpdateKnownLocationPage> createState() =>
      _UpdateKnownLocationPageState();
}

class _UpdateKnownLocationPageState extends State<UpdateKnownLocationPage> {
  // הגדרת קונטרולרים לשדות הטקסט
  late TextEditingController _nameController;
  late TextEditingController _addressController;
  late TextEditingController _radiusController;
  late TextEditingController _latitudeController;
  late TextEditingController _longitudeController;
  bool isLoading = false;

  @override
  void initState() {
    super.initState();
    // אתחול קונטרולרים עם ערכים שמגיעים מה-locationData
    _nameController = TextEditingController(
      text: widget.locationData['locationName']?.toString() ?? '',
    );
    _addressController = TextEditingController(
      text: widget.locationData['address']?.toString() ?? '',
    );
    _radiusController = TextEditingController(
      text: widget.locationData['radius']?.toString() ?? '',
    );
    _latitudeController = TextEditingController(
      text: widget.locationData['latitude']?.toString() ?? '',
    );
    _longitudeController = TextEditingController(
      text: widget.locationData['longitude']?.toString() ?? '',
    );
  }

  @override
  void dispose() {
    // שחרור משאבים של הקונטרולרים
    _nameController.dispose();
    _addressController.dispose();
    _radiusController.dispose();
    _latitudeController.dispose();
    _longitudeController.dispose();
    super.dispose();
  }

  // פונקציה לעדכון נתוני האזור בשרת
  Future<void> _updateLocation() async {
    setState(() {
      isLoading = true;
    });
    final locationId = widget.locationData['locationId'];
    final userId = widget.locationData['userId'];
    final data = {
      // בניית אובייקט האזור לעדכון
      'locationId': locationId,
      'userId': userId,
      'latitude': double.tryParse(_latitudeController.text.trim()) ?? 0.0,
      'longitude': double.tryParse(_longitudeController.text.trim()) ?? 0.0,
      'radius': int.tryParse(_radiusController.text.trim()) ?? 0,
      'address': _addressController.text.trim(),
      'locationName': _nameController.text.trim(),
      'createdAt':
          widget.locationData['createdAt'] ??
          DateTime.now()
              .toUtc()
              .add(const Duration(hours: 3))
              .toIso8601String(),
    };
    print('--- נתונים שנשלחים לעדכון אזור מוכר ---');
    print(data);
    print('-------------------------------');
    try {
      final response = await http.put(
        // שליחת בקשת עדכון לשרת
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/UpdateKnownLocation',
        ),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(data),
      );
      if (response.statusCode == 200) {
        // שמירת נתוני משתמש בלוקל סטורג' לאחר עדכון מוצלח
        try {
          final prefs = await SharedPreferences.getInstance();
          final userData = jsonDecode(response.body);
          if (userData['knownLocation'] != null &&
              userData['knownLocation']['userId'] != null) {
            await prefs.setString(
              'user_id',
              userData['knownLocation']['userId'].toString(),
            );
          }
        } catch (e) {
          print(
            'שגיאה בשמירת נתוני משתמש בלוקל סטורג\' אחרי עדכון אזור מוכר: $e',
          );
        }
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('האזור עודכן בהצלחה!'),
            backgroundColor: Colors.green,
          ),
        );
        Navigator.pop(context);
      } else {
        // טיפול בשגיאה בעדכון
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('שגיאה בעדכון: \\${response.body}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      // טיפול בשגיאת תקשורת
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('שגיאת חיבור לשרת: $e'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      setState(() {
        isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    // בניית ממשק המשתמש של עמוד עריכת אזור מוכר
    return Directionality(
      textDirection: TextDirection.rtl,
      child: BasePage(
        child: Scaffold(
          backgroundColor: const Color(0xFFF8F5FB),
          appBar: AppBar(
            // סרגל עליון עם כפתור חזור ולוגו
            backgroundColor: Colors.transparent,
            elevation: 0,
            leading: IconButton(
              icon: const Icon(Icons.arrow_back_ios, color: Color(0xFF1D2E59)),
              onPressed: () => Navigator.pop(context),
            ),
            title: Image.asset(
              'assets/images/LOGO1.png',
              width: 100,
              height: 60,
            ),
            centerTitle: true,
          ),
          body:
              isLoading
                  ? const Center(
                    child: CircularProgressIndicator(),
                  ) // טוען נתונים
                  : SingleChildScrollView(
                    // טופס עריכת אזור מוכר
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 24.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          const SizedBox(height: 10),
                          const Text(
                            'עריכת אזור מוכר',
                            style: TextStyle(
                              fontSize: 28,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF1D2E59),
                            ),
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 24),
                          _buildTextField(
                            'שם האזור',
                            'לדוג׳: הבית של דני',
                            _nameController,
                          ),
                          const SizedBox(height: 16),
                          _buildTextField(
                            'כתובת',
                            'עיר, רחוב, מספר בית',
                            _addressController,
                          ),
                          const SizedBox(height: 16),
                          _buildTextField(
                            'רדיוס (במטרים)',
                            '300',
                            _radiusController,
                            keyboardType: TextInputType.number,
                          ),
                          const SizedBox(height: 16),
                          _buildTextField(
                            'קו רוחב',
                            '31.2518',
                            _latitudeController,
                            keyboardType: TextInputType.number,
                          ),
                          const SizedBox(height: 16),
                          _buildTextField(
                            'קו אורך',
                            '34.7913',
                            _longitudeController,
                            keyboardType: TextInputType.number,
                          ),
                          const SizedBox(height: 32),
                          // כפתור עדכן אזור - גודל דינאמי סביב הטקסט
                          ElevatedButton(
                            onPressed: _updateLocation, // קריאה לעדכון האזור
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFF1D2E59),
                              foregroundColor: Colors.white,
                              padding: const EdgeInsets.symmetric(
                                vertical: 12,
                                horizontal: 24,
                              ),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                            ),
                            child: const Text(
                              'עדכן אזור',
                              style: TextStyle(fontSize: 15),
                            ),
                          ),
                          const SizedBox(height: 5),
                          // כפתור מחיקת אזור מוכר
                          GestureDetector(
                            onTap: () async {
                              final confirmed = await showDialog<bool>(
                                context: context,
                                builder:
                                    (context) => AlertDialog(
                                      title: const Center(
                                        child: Text('אישור מחיקה'),
                                      ),
                                      content: const Directionality(
                                        textDirection: TextDirection.rtl,
                                        child: Text(
                                          'האם אתה בטוח שברצונך למחוק את האזור המוכר?',
                                          textAlign: TextAlign.right,
                                        ),
                                      ),
                                      actions: [
                                        TextButton(
                                          onPressed:
                                              () => Navigator.of(
                                                context,
                                              ).pop(false),
                                          child: const Text('ביטול'),
                                        ),
                                        TextButton(
                                          onPressed:
                                              () => Navigator.of(
                                                context,
                                              ).pop(true),
                                          child: const Text('אישור'),
                                        ),
                                      ],
                                    ),
                              );
                              if (confirmed == true) {
                                final locationId =
                                    widget.locationData['locationId'];
                                final userId = widget.locationData['userId'];
                                final url = Uri.parse(
                                  'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/DeleteKnownLocation?location_id=$locationId&user_id=$userId',
                                );
                                try {
                                  final response = await http.delete(url);
                                  if (response.statusCode == 200) {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      const SnackBar(
                                        content: Text('האזור נמחק בהצלחה!'),
                                        backgroundColor: Colors.red,
                                      ),
                                    );
                                    Navigator.pop(context, true);
                                  } else {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      SnackBar(
                                        content: Text(
                                          'שגיאה במחיקת האזור: ${response.body}',
                                        ),
                                        backgroundColor: Colors.red,
                                      ),
                                    );
                                  }
                                } catch (e) {
                                  ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(
                                      content: Text('שגיאת חיבור לשרת'),
                                      backgroundColor: Colors.red,
                                    ),
                                  );
                                }
                              }
                            },
                            child: Container(
                              width: double.infinity,
                              padding: const EdgeInsets.symmetric(vertical: 12),
                              child: const Text(
                                'מחיקת אזור מוכר',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  color: Colors.red,
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  decoration: TextDecoration.underline,
                                  decorationColor: Colors.red,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(height: 24),
                        ],
                      ),
                    ),
                  ),
        ),
      ),
    );
  }

  // בניית שדה טקסט עם תווית
  Widget _buildTextField(
    String label,
    String hint,
    TextEditingController controller, {
    TextInputType? keyboardType,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        TextField(
          controller: controller,
          keyboardType: keyboardType,
          style: const TextStyle(fontSize: 15, color: Color(0xFF1D2E59)),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: const TextStyle(fontSize: 14, color: Color(0xFF7B8FA1)),
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
          ),
        ),
      ],
    );
  }
}
