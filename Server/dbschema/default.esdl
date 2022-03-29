module default {
  type Authorization {
    required property name -> str {
      constraint exclusive;
    }
    required property key -> str {
      constraint exclusive;
    }
  }
}
