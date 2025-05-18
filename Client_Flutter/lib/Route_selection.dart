// ignore_for_file: depend_on_referenced_packages

import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'base_page.dart'; // ייבוא BasePage

class RouteSelectionPage extends StatelessWidget {
  const RouteSelectionPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20.0),
          child: SingleChildScrollView(
            child: Column(
              children: [
                const SizedBox(height: 40), // ריווח נוסף בראש העמוד
                Image.asset(
                  'assets/images/LOGO1.png', // ודא שהנתיב לתמונה נכון
                  width: 100,
                  height: 100,
                ),
                const SizedBox(height: 10),
                const Text(
                  'בחירת מסלול',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 20),
                _buildTextField('הזן כתובת התחלה', 'התיכון 7, נשר'),
                const SizedBox(height: 10),
                _buildTextField('הזן כתובת יעד', 'עמוס 18, נשר'),
                const SizedBox(height: 20),
                Container(
                  height: 200, // גובה קטן יותר למפה
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: Colors.grey),
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(10),
                    child: GoogleMap(
                      initialCameraPosition: const CameraPosition(
                        target: LatLng(
                          32.794044,
                          34.989571,
                        ), // מיקום התחלתי (נשר)
                        zoom: 14.0,
                      ),
                      markers: {
                        Marker(
                          markerId: const MarkerId('start'),
                          position: const LatLng(
                            32.794044,
                            34.989571,
                          ), // נקודת התחלה
                          infoWindow: const InfoWindow(title: 'התיכון 7, נשר'),
                        ),
                        Marker(
                          markerId: const MarkerId('end'),
                          position: const LatLng(
                            32.793200,
                            34.989000,
                          ), // נקודת יעד
                          infoWindow: const InfoWindow(title: 'עמוס 18, נשר'),
                        ),
                      },
                      onMapCreated: (GoogleMapController controller) {
                        // ניתן להוסיף כאן פעולות נוספות
                      },
                    ),
                  ),
                ),
                const SizedBox(height: 20),
                ElevatedButton(
                  onPressed: () {
                    // הוסף כאן את הפעולה לבחירת מסלול
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
                      horizontal: 40,
                      vertical: 15,
                    ),
                  ),
                  child: const Text(
                    'בחר מסלול',
                    style: TextStyle(fontSize: 16),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTextField(String label, String hint) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 40,
          child: TextField(
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
            ),
          ),
        ),
      ],
    );
  }
}
