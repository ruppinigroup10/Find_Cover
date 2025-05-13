import 'package:fc_flutter/Enter.dart';
import 'package:flutter/material.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false, // הסרת הבאנר של Debug
      title: 'FC Flutter',
      theme: ThemeData(primarySwatch: Colors.blue),
      home: const EnterPage(
        //title: 'Settings', // הכותרת של העמוד
      ),
    );
  }
}
