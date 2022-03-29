CREATE MIGRATION m17xfvjennpwad3bx544bbntxfcpr6vjcgrnirnv7xoxbmy4zkdxqa
    ONTO initial
{
  CREATE TYPE default::Authorization {
      CREATE REQUIRED PROPERTY key -> std::str;
  };
};
